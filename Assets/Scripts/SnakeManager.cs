using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class SnakeManager : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private float snakeMoveInterval = 0.5f;
    private float modifiedInterval;
    [SerializeField] private int tileCount = 10; // 10 means a 10x10 world
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float scaleFactor = 1.1f;
    private int timer = 20;

    [Header("Refs")]
    [SerializeField] private GameObject worldHolder;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject fruit1, fruit2;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Material whiteMat;
    [SerializeField] private ParticleSystem explodeParticles;
    [SerializeField] private SpriteRenderer backgroundSR;
    [SerializeField] private GameObject foodParticles;
    [SerializeField] private GameObject continueButton;
    private int round, score, highscore;
    private int dUpgrade, eUpgrade, wUpgrade;
    [SerializeField] private TextMeshProUGUI roundText, scoreText, highscoreText;
    [SerializeField] private Button dUpgradeButton, eUpgradeButton, wUpgradeButton;
    [SerializeField] private Toggle dUpgradeToggle, eUpgradeToggle, wUpgradeToggle;

    [Header("Sprites")]
    [SerializeField] private List<Sprite> backgroundList;
    // Outside list is parts, inside list is directions
    // HeadN, HeadE, HeadS, HeadW
    // MiddleN, MiddleE, etc
    [SerializeField] private List<SpriteList> spritesList;
    [SerializeField] private List<SpriteList> bulgeSpritesList;
    [SerializeField] private List<SpriteList> eatAnimation;

    private static SnakeManager instance;
    public static SnakeManager Instance { get { return instance; } }

    // The list of actual sprite renderers in the level
    private List<List<SpriteRenderer>> snakeRenderers = new();
    // The list of where snake sections are in the world
    private List<List<SnakeSection>> snakeSections = new();

    // A list of the snake parts from head to tail, the Vector2 holds where in the snakeRenderers they are
    private List<Vector2Int> snakePartIndices = new();
    private List<Vector2Int> snakeBulgeLocations = new();
    private List<int> snakeBulgeIndices = new();

    // For now, we start facing/moving to the right (like the google snake game lol)
    private Vector2Int facingDirection = new Vector2Int(1, 0);

    // If the player put in an input to turn but didn't actually turn yet (due to move only being updated every so often)
    private bool tryingToTurn;

    private Vector2Int fruit1Pos = new Vector2Int(7, 5);
    private Vector2Int fruit2Pos = new Vector2Int(8, 5);
    // The square the tail of the snake just left, which will be used if a fruit is eaten
    private Vector2Int lastLeftSquare;
    bool eating;
    int frame;


    private void Awake()
    {
        if (instance != null && instance != this) Destroy(gameObject);
        else instance = this;
    }

    void Start()
    {
        timer = PlayerPrefs.GetInt("eupgrade") == 1 ? 40 : 20;
        timerSlider.maxValue = timer;

        backgroundList = backgroundList.OrderBy(x => Random.value).ToList();
        modifiedInterval = snakeMoveInterval / 2f;

        roundText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        highscoreText.gameObject.SetActive(false);

        if (PlayerPrefs.HasKey("highscore")) highscore = PlayerPrefs.GetInt("highscore");
        else PlayerPrefs.SetInt("highscore", highscore = 0);
        if (PlayerPrefs.HasKey("round")) round = PlayerPrefs.GetInt("round") + 1;
        else PlayerPrefs.SetInt("round", round = 0);

        if (PlayerPrefs.HasKey("dupgrade")) dUpgrade = PlayerPrefs.GetInt("dupgrade");
        else PlayerPrefs.SetInt("dupgrade", dUpgrade = 0);

        if (PlayerPrefs.HasKey("eupgrade")) dUpgrade = PlayerPrefs.GetInt("eupgrade");
        else PlayerPrefs.SetInt("eupgrade", dUpgrade = 0);

        if (PlayerPrefs.HasKey("wupgrade")) dUpgrade = PlayerPrefs.GetInt("wupgrade");
        else PlayerPrefs.SetInt("wupgrade", dUpgrade = 0);

        dUpgradeToggle.isOn = PlayerPrefs.GetInt("dupgrade") == 1;
        eUpgradeToggle.isOn = PlayerPrefs.GetInt("eupgrade") == 1;
        wUpgradeToggle.isOn = PlayerPrefs.GetInt("wupgrade") == 1;



        roundText.text = $"attempt #{round:D3}";
        highscoreText.text = $"highscore: {highscore:D3}";

        continueButton.SetActive(false);

        // Fill up arrays with Blank values
        FillArrays();
        // Fill the world with empty sprite renderers
        CreateObjectRenderers();
        // Add in the snake

        snakePartIndices.Add(new Vector2Int(1, 5));
        snakePartIndices.Add(new Vector2Int(0, 5));
    }

    public void StartAnimation() => StartCoroutine(nameof(StartAnimationCoroutine));
    private IEnumerator StartAnimationCoroutine()
    {
        StartCoroutine(nameof(StartBackgroundAnimation));
        yield return new WaitForSeconds(1f);

        // Move the snake every moveInterval seconds
        InvokeRepeating("TryMoveSnake", 0, snakeMoveInterval);

        yield return new WaitForSeconds(0.1f);
        roundText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        scoreText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        highscoreText.gameObject.SetActive(true);
    }
    private IEnumerator StartBackgroundAnimation()
    {
        foreach (var s in backgroundList)
        {
            backgroundSR.sprite = s;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void FillArrays()
    {
        SnakeSection blank = new SnakeSection();
        blank.direction = Direction.Blank;
        blank.part = SnakePart.Blank;

        for (int x = 0; x < tileCount; x++)
        {
            snakeSections.Add(new List<SnakeSection>());
            for (int y = 0; y < tileCount; y++)
            {
                snakeSections[x].Add(blank);
            }
        }
    }

    private void CreateObjectRenderers()
    {
        for (int x = 0; x < tileCount; x++)
        {
            snakeRenderers.Add(new List<SpriteRenderer>());
            for (int y = 0; y < tileCount; y++)
            {
                snakeRenderers[x].Add(new SpriteRenderer());
            }
        }
        for (int x = 0; x < tileCount; x++)
        {
            for (int y = 0; y < tileCount; y++)
            {
                GameObject newTile = Instantiate(tilePrefab, worldHolder.transform);
                newTile.transform.position = worldHolder.transform.position - new Vector3(tileSize * tileCount, tileSize * tileCount, 0) / 2 + new Vector3(x * tileSize, y * tileSize, 0);
                newTile.transform.localScale = new Vector2(tileSize, tileSize) * scaleFactor;
                snakeRenderers[x][y] = newTile.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void Update()
    {
        GetInput();
        if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
    }
    public void RestartLevel() => StartCoroutine(nameof(RestartLevelCoroutine));
    private IEnumerator RestartLevelCoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void TryMoveSnake()
    {


        // If we can move the snake, move it and render it
        if (CanMoveSnake())
        {
            MoveSnake();
            RenderSnake();
            CheckFruit();
            RenderFruit();
        }
        // Otherwise, the player loses
        else
        {
            GameOver();
        }
        if (timer < 0) GameOver();
    }


    private void GetInput()
    {
        Vector2Int lastFacingDirection = facingDirection;

        int horizontal = Mathf.CeilToInt(Input.GetAxisRaw("Horizontal"));
        int vertical = Mathf.CeilToInt(Input.GetAxisRaw("Vertical"));

        if (horizontal == 0 && vertical == 0)
        {
            horizontal = facingDirection.x;
            vertical = facingDirection.y;
        }
        else if (Mathf.Abs(horizontal) == 1 && Mathf.Abs(vertical) == 1)
        {
            vertical = 0;
        }

        if (!new Vector2Int(horizontal, vertical).Equals(-lastFacingDirection) && !tryingToTurn)
        {
            facingDirection = new Vector2Int(horizontal, vertical);
        }

        if (!lastFacingDirection.Equals(facingDirection))
        {
            tryingToTurn = true;
            Camera.main.transform.DOPunchScale(Vector3.one * -0.025f, 0.10f, 1);

            string s;
            switch (facingDirection)
            {
                case Vector2Int v when v.Equals(Vector2Int.up): s = "snakeTurnN"; break;
                case Vector2Int v when v.Equals(Vector2Int.right): s = "snakeTurnE"; break;
                case Vector2Int v when v.Equals(Vector2Int.down): s = "snakeTurnS"; break;
                default: s = "snakeTurnW"; break;
            }
            SoundManager.Instance.PlaySoundEffect(s);
        }
    }

    private bool CanMoveSnake()
    {
        // Return whether or not the snake can move, if it can't, player loses
        Vector2Int nextPosition = snakePartIndices[0] + facingDirection;
        bool wouldBeInBounds = nextPosition.x < tileCount && nextPosition.y < tileCount && nextPosition.x >= 0 && nextPosition.y >= 0;

        bool wouldNotCollideWithSelf = true;
        foreach (Vector2Int snakePartPos in snakePartIndices)
        {
            if (snakePartPos == nextPosition)
            {
                wouldNotCollideWithSelf = false;
                break;
            }
        }

        return wouldBeInBounds && wouldNotCollideWithSelf;
    }


    private void MoveSnake()
    {

        UpdateTimer();

        if (eating) frame++;
        if (frame == 3) eating = false;

        // THEORY
        // Head moves forward in the direction we're facing
        // Each body segment moves into where the next body segment is/was

        // How do we also set which part of the snake this should be?
        // Head is always head
        // Other parts will check where the next part is in relation to them, then use that to determine what they should be
        // The last part will be tail


        // Clear out the snakeSections list, we'll be replacing it
        snakeSections.Clear();
        FillArrays();

        Vector2Int partLastPosition = Vector2Int.zero;
        for (int i = 0; i < snakePartIndices.Count; i++)
        {
            // If this is the first section, this is the head
            if (i == 0)
            {
                // Set partLastPosition to this
                // This is so that the next section after the head can move to where the head is
                partLastPosition = snakePartIndices[i];
                // Move the head in the facing direction
                snakePartIndices[i] += facingDirection;

                // Set the value of snakeSections at this snakePartIndex to head and set direction
                SnakeSection sectionToModify = snakeSections[snakePartIndices[i].x][snakePartIndices[i].y];
                sectionToModify.part = SnakePart.Head;
                sectionToModify.direction = (Direction)PartDirectionIndexFromVector2(facingDirection);
                snakeSections[snakePartIndices[i].x][snakePartIndices[i].y] = sectionToModify;
            }
            // Otherwise it's one of the body sections
            else
            {
                // Set where the part currently is
                Vector2Int partCurrentPosition = snakePartIndices[i];
                // Move the part
                snakePartIndices[i] = partLastPosition;
                // Set partLastPosition to where the part just moved from
                partLastPosition = partCurrentPosition;

                // Set the value of snakeSections at this snakePartIndex to what it should be
                SnakeSection sectionToModify = snakeSections[snakePartIndices[i].x][snakePartIndices[i].y];

                Vector2Int pointingToPreviousPart = snakePartIndices[i - 1] - snakePartIndices[i];
                Vector2Int pointingToNextPart = new Vector2Int(0, 0);
                if (i + 1 < snakePartIndices.Count)
                    pointingToNextPart = snakePartIndices[i + 1] - snakePartIndices[i];

                // Figure out which part it should be
                if (i == snakePartIndices.Count - 1)
                {
                    sectionToModify.part = SnakePart.Tail;
                }
                else if (ArePartsInLine(snakePartIndices[i], snakePartIndices[i] + pointingToPreviousPart, partLastPosition))
                {
                    sectionToModify.part = SnakePart.Straight;
                }
                else
                {
                    if (TurningRight(pointingToPreviousPart, pointingToNextPart))
                    {
                        sectionToModify.part = SnakePart.Corner;
                    }
                    else
                    {
                        sectionToModify.part = SnakePart.InverseCorner;
                    }
                }
                // Use the difference between this part and the part before it (closer to the head) to determine the direction
                sectionToModify.direction = (Direction)PartDirectionIndexFromVector2(pointingToPreviousPart);
                snakeSections[snakePartIndices[i].x][snakePartIndices[i].y] = sectionToModify;
            }
        }
        lastLeftSquare = partLastPosition;
        tryingToTurn = false;
    }

    private bool TurningRight(Vector2Int pointingToPrevious, Vector2Int pointingToNext)
    {
        if ((pointingToPrevious.x < 0 && pointingToNext.y > 0) || (pointingToPrevious.x > 0 && pointingToNext.y < 0) || (pointingToPrevious.y > 0 && pointingToNext.x > 0) || (pointingToPrevious.y < 0 && pointingToNext.x < 0))
        {
            return true;
        }
        return false;
    }

    private int PartDirectionIndexFromVector2(Vector2Int dir)
    {
        if (dir.x == 0)
        {
            if (dir.y > 0)
            {
                return 0;
            }
            else if (dir.y < 0)
            {
                return 2;
            }
        }
        else if (dir.x > 0)
        {
            return 1;
        }
        else if (dir.x < 0)
        {
            return 3;
        }

        Debug.LogError("The direction you supplied was not valid: ");
        Debug.LogError(dir);
        return 0;
    }

    private bool ArePartsInLine(Vector2Int currentPos, Vector2Int previousPartPosition, Vector2Int nextPartPosition)
    {
        bool allYsDifferent = currentPos.y != previousPartPosition.y && currentPos.y != nextPartPosition.y && previousPartPosition.y != nextPartPosition.y;
        bool allXsDifferent = currentPos.x != previousPartPosition.x && currentPos.x != nextPartPosition.x && previousPartPosition.x != nextPartPosition.x;

        // Return Straight if the Xs or Ys are all different (and thus in a line), otherwise Corner
        return allXsDifferent || allYsDifferent;
    }


    private void RenderSnake()
    {
        /*var newList = new List<int>();
        foreach (var i in snakeBulgeIndices)
        {
            if(i+1 < snakePartIndices.Count - 1) newList.Add(i + 1);
        }
        snakeBulgeIndices = newList;*/
        for (int i = 0; i < snakeBulgeIndices.Count; i++)
        {
            var val = snakeBulgeIndices[i];
            if (val + 1 < snakePartIndices.Count - 1) snakeBulgeIndices[i] = val;
            else snakePartIndices.RemoveAt(i);
        }
        for (int i = 0; i < snakeBulgeLocations.Count; i++)
        {

        }

        /*var newBulgeList = new List<Vector2Int>();
        foreach (var i in snakeBulgeIndices)
            newBulgeList.Add(snakePartIndices[i]);
        snakeBulgeLocations = newBulgeList;*/

        //snakeBulgeIndices = newList;
        /*foreach (var i in snakeBulgeIndices)
            Debug.Log($"<color=blue>{i}</color>");*/

        //foreach (var z in newList) Debug.Log(z);

        foreach (List<SpriteRenderer> listOfSR in snakeRenderers)
        {
            foreach (SpriteRenderer sr in listOfSR)
            {
                sr.enabled = false;
            }
        }

        int x = 0;
        int y = 0;
        var counter = 0;
        foreach (List<SnakeSection> snakeSectionList in snakeSections)
        {
            foreach (SnakeSection snakeSection in snakeSectionList)
            {
                if (snakeSection.direction != Direction.Blank && snakeSection.part != SnakePart.Blank)
                {
                    RenderSnakeSection(snakeSection, x, y, counter);
                    Debug.Log($"<color=green>{counter}</color>");
                    counter++;
                }
                y++;
            }
            y = 0;
            x++;
        }
    }

    private void RenderSnakeSection(SnakeSection section, int x, int y, int counter)
    {
        List<SpriteList> list = spritesList;
        /*if (snakeBulgeIndices.Contains(counter))
        {
            list = bulgeSpritesList;
            if (section.part.Equals(SnakePart.Tail))
                snakeBulgeIndices.Remove(counter);
            else list = bulgeSpritesList;
        }*/
        if (snakeBulgeLocations.Contains(new Vector2Int(x, y)))
        {
            if (section.part.Equals(SnakePart.Tail))
                snakeBulgeLocations.Remove(new Vector2Int(x, y));
            else list = bulgeSpritesList;
        }

        snakeRenderers[x][y].sprite = (eating && section.part.Equals(SnakePart.Head)) ? eatAnimation[frame].spritesList[(int)section.direction] : list[(int)section.part].spritesList[(int)section.direction];
        snakeRenderers[x][y].enabled = true;
    }


    private void CheckFruit()
    {
        // Check if the heads square is the same as the fruit square
        if (snakePartIndices[0].Equals(fruit1Pos))
        {
            // If it is, increment score, lengthen snake and RespawnFruit()
            snakeBulgeLocations.Add(fruit1Pos);
            snakeBulgeIndices.Add(0);
            SoundManager.Instance.AdvanceMusic();

            var p = Instantiate(foodParticles, snakeRenderers[fruit1Pos.x][fruit1Pos.y].transform.position, Quaternion.identity);
            Destroy(p, 1f);

            ResetTimer();
            score++;
            if (score > PlayerPrefs.GetInt("highscore")) PlayerPrefs.SetInt("highscore", score);

            scoreText.text = $"score: {score:D3}";
            highscoreText.text = $"highscore: {highscore:D3}";

            snakePartIndices.Add(lastLeftSquare);
            RespawnFruit1();
        }

        if(PlayerPrefs.GetInt("dupgrade") == 1 && snakePartIndices[0].Equals(fruit2Pos))
        {
            // If it is, increment score, lengthen snake and RespawnFruit()
            snakeBulgeLocations.Add(fruit2Pos);
            snakeBulgeIndices.Add(0);
            SoundManager.Instance.AdvanceMusic();

            var p = Instantiate(foodParticles, snakeRenderers[fruit1Pos.x][fruit1Pos.y].transform.position, Quaternion.identity);
            Destroy(p, 1f);

            ResetTimer();
            score++;
            if (score > PlayerPrefs.GetInt("highscore")) PlayerPrefs.SetInt("highscore", score);

            scoreText.text = $"score: {score:D3}";
            highscoreText.text = $"highscore: {highscore:D3}";

            snakePartIndices.Add(lastLeftSquare);
            RespawnFruit2();
        }
    }

    private void RespawnFruit1()
    {
        // Collect a list of the empty squares
        // Randomly select one of them
        // Show the fruit there
        SoundManager.Instance.PlaySoundEffect("snakeEat");
        frame = 0;
        eating = true;

        int x = 0;
        int y = 0;
        List<Vector2Int> emptySquares = new List<Vector2Int>();

        foreach (List<SpriteRenderer> srList in snakeRenderers)
        {
            foreach (SpriteRenderer sr in srList)
            {
                if (!sr.enabled)
                {
                    emptySquares.Add(new Vector2Int(x, y));
                }
                y++;
            }
            y = 0;
            x++;
        }

        int randSquare = Random.Range(0, emptySquares.Count - 1);
        fruit1Pos = emptySquares[randSquare];
        fruit1.SetActive(false);
        fruit1.SetActive(true);
    }

    private void RespawnFruit2()
    {
        // Collect a list of the empty squares
        // Randomly select one of them
        // Show the fruit there
        SoundManager.Instance.PlaySoundEffect("snakeEat");
        frame = 0;
        eating = true;

        int x = 0;
        int y = 0;
        List<Vector2Int> emptySquares = new List<Vector2Int>();

        foreach (List<SpriteRenderer> srList in snakeRenderers)
        {
            foreach (SpriteRenderer sr in srList)
            {
                if (!sr.enabled)
                {
                    emptySquares.Add(new Vector2Int(x, y));
                }
                y++;
            }
            y = 0;
            x++;
        }

        int randSquare = Random.Range(0, emptySquares.Count - 1);
        fruit2Pos = emptySquares[randSquare];
        fruit2.SetActive(false);
        fruit2.SetActive(true);
    }

    private void RenderFruit()
    {
        //snakeRenderers[fruitPos.x][fruitPos.y].sprite = fruitSprite;
        //snakeRenderers[fruitPos.x][fruitPos.y].enabled = true;
        fruit1.transform.position = snakeRenderers[fruit1Pos.x][fruit1Pos.y].transform.position;
        Debug.Log(PlayerPrefs.GetInt("dupgrade"));
        if(PlayerPrefs.GetInt("dupgrade") == 1) fruit2.transform.position = snakeRenderers[fruit2Pos.x][fruit2Pos.y].transform.position;

        //var anim = fruit.GetComponent<Animator>();
        //anim.Play()
        //fruit.SetActive(false);
        //fruit.SetActive(true);
    }

    private void GameOver()
    {
        continueButton.SetActive(true);

        SoundManager.Instance.ResetMusic();

        GameOverTimer();
        GameOverAnimation();
        Camera.main.transform.DOShakePosition(0.15f, 0.5f, 50);
        // TODO: Show the player their score
        CancelInvoke();
        Debug.Log("GAME OVER");
    }

    private void GameOverAnimation() => StartCoroutine(nameof(GameOverAnimationCoroutine));
    private IEnumerator GameOverAnimationCoroutine()
    {
        var c = 0;
        foreach (var s in snakePartIndices)
        {
            SoundManager.Instance.PlaySoundEffect("snakeDie", c);

            //Debug.Log($"{s.x},{s.y}");

            var sr = snakeRenderers[s.x][s.y];
            var p = Instantiate(explodeParticles, sr.transform.position, Quaternion.identity);
            var pMain = p.main;
            pMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, Mathf.Clamp((float)c / snakePartIndices.Count, 0.1f, 1));

            sr.material = whiteMat;
            sr.transform.DOScale(Vector2.zero, 0.1f);
            yield return new WaitForSeconds(0.1f);
            c++;
        }
        yield return null;
    }

    private void UpdateTimer()
    {
        if (timer >= 0)
        {
            timerText.text = timer--.ToString();
            timerSlider.value = timer;
        }
    }
    private void GameOverTimer()
    {
        timerText.text = "GAME OVER";
        timerSlider.value = 0;
    }

    private void ResetTimer()
    {
        timer = PlayerPrefs.GetInt("eupgrade") == 1 ? 40 : 20;
        var image = timerSlider.fillRect.GetComponent<Image>();
        var prevColor = image.color;
        image.DOColor(Color.white, snakeMoveInterval / 2f).OnComplete(() => image.DOColor(prevColor, snakeMoveInterval / 2f));
    }

    public void GoToUpgrades()
    {
        Camera.main.transform.DOMove(Camera.main.transform.position + (Vector3)Vector2.left * 30f, 1f);
    }

    public void ToggleUpgrade(string name)
    {
        PlayerPrefs.SetInt(name, (PlayerPrefs.GetInt(name) == 1) ? 0 : 1);
    }
}

[System.Serializable]
public class SpriteList
{
    [SerializeField] private string name;
    [SerializeField]
    public List<Sprite> spritesList;
}

struct SnakeSection
{
    public SnakePart part;
    public Direction direction;
}

enum SnakePart
{
    Head,
    Straight,
    Corner,
    Tail,
    InverseCorner,
    Blank
}

enum Direction
{
    North,
    East,
    South,
    West,
    Blank
}