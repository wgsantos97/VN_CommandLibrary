using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class NovelController : MonoBehaviour
{
    /// <summary>
    /// Call this event whenever novelController is finished reading the txt file
    /// </summary>
    public event EventHandler chapterStarted;
    /// <summary>
    /// Call this event whenever novelController is finished reading the txt file
    /// </summary>
    public event EventHandler chapterFinished;

    public bool defaultStart = false;
    public string fileName = "";

    /// <summary> The lines of data loaded directly from a chapter file. </summary>
    List<string> data = new List<string>();
    static VN_CommandLibrary VN_CommandLibrary;
    public static NovelController instance;
    void Awake()
    {
        if (VN_CommandLibrary == null)
            VN_CommandLibrary = new VN_CommandLibrary();
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (defaultStart)
            LoadChapterFile(fileName);
    }

    // Update is called once per frame
    void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            Next();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Skip();
        }
    }

    public void LoadChapterFile(string fileName)
    {
        //data = FileManager.LoadFile(FileManager.savPath + "Resources/Story/" + fileName);
        TextAsset textAsset = Resources.Load<TextAsset>("Story/" + fileName);
        data = textAsset.text.Split('\n').ToList();
        cachedLastSpeaker = "";

        if (handlingChapterFile != null)
            StopCoroutine(handlingChapterFile);
        handlingChapterFile = StartCoroutine(HandlingChapterFile());

        // auto start the chapter
        Next();
    }

    /// <summary> Trigger that advances the progress through a chapter file. </summary>
    bool _next = false;
    public void Next()
    {
        _next = true;
    }

    /// <summary> Jumps to the end of a chapter file. </summary>
    public void Skip()
    {
        chapterProgress = data.Count - 1;
        Next();
    }

    public bool isHandlingChapterFile { get { return handlingChapterFile != null; } }
    Coroutine handlingChapterFile = null;
    [HideInInspector] public int chapterProgress = 0;
    IEnumerator HandlingChapterFile()
    {
        // the progress through the lines in this chapter
        chapterProgress = 0;

        if (chapterStarted != null)
            chapterStarted.Invoke(this, new EventArgs());

        while (chapterProgress < data.Count)
        {
            // we need a way of knowing  when the player wants to advance. We need a "next" trigger.
            // Not just a keypress. But something that can be triggered by a click or keypress.
            if (_next)
            {
                string line = data[chapterProgress];

                if (line.StartsWith("choice"))
                {
                    yield return HandlingChoiceLine(line);
                    chapterProgress++;
                }
                else
                {
                    HandleLine(line);
                    chapterProgress++;
                    while (isHandlingLine)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }
        if (chapterFinished != null)
            chapterFinished.Invoke(this, new EventArgs());
        handlingChapterFile = null;
    }

    IEnumerator HandlingChoiceLine(string line)
    {
        string title = line.Split('"')[1];
        List<string> choices = new List<string>();
        List<string> actions = new List<string>();

        while (true)
        {
            chapterProgress++;
            line = data[chapterProgress];

            if (line == "{")
                continue;

            line = line.Replace("    ", ""); // removes the tabs that have become quad spaces.

            if (line != "}")
            {
                choices.Add(line.Split('"')[1]);
                actions.Add(data[chapterProgress + 1].Replace("    ", ""));
                chapterProgress++;
            }
            else
            {
                break;
            }
        }

        // display choices
        if (choices.Count > 0)
        {
            ChoiceScreen.Show(title, choices.ToArray());
            yield return new WaitForEndOfFrame();
            while (ChoiceScreen.isWaitingForChoiceToBeMade)
                yield return new WaitForEndOfFrame();

            // Choice is made. Execute the paired action.
            string action = actions[ChoiceScreen.lastChoiceMade.index];
            HandleLine(action);

            while (isHandlingLine)
                yield return new WaitForEndOfFrame();
        }
        else
        {
            Debug.LogError("Invalid choice operation. " + choices.Count + " choices were found!");
        }
    }

    void HandleLine(string rawLine)
    {
        ChapterLineManager.LINE line = ChapterLineManager.Interpret(rawLine);

        // now we have to handle the line. this requires a loop full of waiting for input since the 
        // line consists of multiple segments that have to be handled individually.
        StopHandlingLine();
        handlingLine = StartCoroutine(HandlingLine(line));
    }

    void StopHandlingLine()
    {
        if (isHandlingLine)
            StopCoroutine(handlingLine);
        handlingLine = null;
    }

    public bool isHandlingLine { get { return handlingLine != null; } }
    Coroutine handlingLine = null;
    IEnumerator HandlingLine(ChapterLineManager.LINE line)
    {
        // since the "next" trigger controls the flow of a chapter by moving through lines and yet also
        // controls the progression through a line by its segments, it must be reset.
        _next = false;
        int lineProgress = 0; // progress through the segments of a line.

        while (lineProgress < line.segments.Count)
        {
            _next = false; // reset at the start of each loop.
            ChapterLineManager.LINE.SEGMENT segment = line.segments[lineProgress];

            // always run the first segment automatically. But wait for the trigger on all proceding segments.
            if (lineProgress > 0)
            {
                if (segment.trigger == ChapterLineManager.LINE.SEGMENT.TRIGGER.autoDelay)
                {
                    for (float timer = segment.autoDelay; timer >= 0; timer -= Time.deltaTime)
                    {
                        yield return new WaitForEndOfFrame();
                        if (_next)
                            break; // allow termination of a delay when "next" is triggered. Prevents unskippable wait timers.
                    }
                }
                else
                {
                    while (!_next)
                        yield return new WaitForEndOfFrame(); // wait until the player says to move to the next segment.
                }
            }
            _next = false; // next could have been triggered during an event above.

            // the segment now needs to build and run.
            segment.Run();

            while (segment.isRunning)
            {
                yield return new WaitForEndOfFrame();
                // allow for auto completion of the current segment for skipping purposes.
                if (_next)
                {
                    // rapidly complete the text on first advance, force it to finish on the second.
                    if (!segment.architect.skip)
                    {
                        segment.architect.skip = true;
                    }
                    else
                    {
                        segment.ForceFinish();
                    }
                    _next = false;
                }
            }

            lineProgress++;

            yield return new WaitForEndOfFrame();
        }

        // Line is finished. Handle all the actions set at the end of the line.
        for (int i = 0; i < line.actions.Count; i++)
        {
            HandleAction(line.actions[i]);
        }
        handlingLine = null;
    }

    [HideInInspector]
    /// <summary> Used as a fallback when no speaker is given. </summary>
    public string cachedLastSpeaker = "";


    public void HandleAction(string action)
    {
        // print("Handle Action: [" + action + "]"); // NOTE: Debug using this print to determine action.
        string[] localData = action.Split('(', ')');
        if (VN_CommandLibrary.commandLibrary.ContainsKey(localData[0]))
        { VN_CommandLibrary.commandLibrary[localData[0]].Call_Command(localData[1]); }
    }
}
