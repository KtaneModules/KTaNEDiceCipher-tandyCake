using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class DiceCipherScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable roll;
    public KMSelectable submit;

    public GameObject[] Die1Pips, Die2Pips, Die3Pips, Die4Pips;
    private GameObject[][] allPips = new GameObject[4][];
    public GameObject[] sidePips;


    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    

    int[][] pipPatterns = new int[][] { new int[] { 3 }, new int[] { 0 }, new int[] { 1 }, new int[] { 2 }, new int[] { 4 }, new int[] { 7 }, new int[] { 6 }, new int[] { 5 }, new int[] { 0, 7 }, new int[] { 0, 7 }, new int[] { 1, 6 }, new int[] { 2, 5 }, new int[] { 3, 4 }, new int[] { 0, 5 }, new int[] { 0, 2 }, new int[] { 2, 7 }, new int[] { 5, 7 }, new int[] { 0, 5, 7 }, new int[] { 1, 3, 6 }, new int[] { 0, 2, 5 }, new int[] { 1, 3, 4 }, new int[] { 1, 3, 4 }, new int[] { 0, 2, 7 }, new int[] { 1, 4, 6 }, new int[] { 2, 5, 7 }, new int[] { 3, 4, 6 } };
    string[] groups = new string[] { "ABIJKRS", "CDLMTUV", "EFNOWX", "GHPQYZ" };
    char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    int chosenGroup;
    int submitGroup;

    string displayed;

    void Awake ()
    {
        moduleId = moduleIdCounter++;
        roll.OnInteract += delegate () { RollDice(); return false; };
        submit.OnInteract += delegate () { Submit(); return false; };
        allPips[0] = Die1Pips;
        allPips[1] = Die2Pips;
        allPips[2] = Die3Pips;
        allPips[3] = Die4Pips;
    }

    void Start ()
    {
        chosenGroup = UnityEngine.Random.Range(0, 4);
        submitGroup = (Bomb.GetBatteryHolderCount() + Bomb.GetIndicators().Count()) % 4;
        Debug.LogFormat("[Dice Cipher #{0}] The common group on the module is group {1}. This is also the index of the target die.", moduleId, chosenGroup + 1);
        Debug.LogFormat("[Dice Cipher #{0}] The target die must be in group {1}.", moduleId, submitGroup + 1);
        RollDice();
    }

    void RollDice()
    {
        roll.AddInteractionPunch(1);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, roll.transform);
        Audio.PlaySoundAtTransform("Roll", transform);
        displayed = WordList.phrases.Where(x => GetGroup(x).Contains(chosenGroup.ToString())).PickRandom();
        Debug.LogFormat("[Dice Cipher #{0}] The dice have been rolled, the displayed word is {1}.", moduleId, displayed);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 8; j++)
                allPips[i][j].SetActive(false); //turns off all the pips
            foreach (int pip in pipPatterns[Array.IndexOf(alphabet, displayed[i])]) 
                allPips[i][pip].SetActive(true); //turns on the pips which are in the correct 
        }
        for (int i = 0; i < 48; i++)
            sidePips[i].SetActive(UnityEngine.Random.Range(0,2) == 1);
    }
    void Submit()
    {
        submit.AddInteractionPunch(1);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        if (moduleSolved) return;
        else if (CheckAnswer(displayed))
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Debug.LogFormat("[Dice Cipher #{0}] {1} has been submitted, which is correct. Module solved.", moduleId, displayed);
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Dice Cipher #{0}] {1} has been submitted, which is incorrect. Strike incurred.", moduleId, displayed);
        }
    }

    int GetGroup(char input)
    {
        for (int i = 0; i < 4; i++)
            if (groups[i].Contains(input))
                return i;
        throw new ArgumentException();
    }
    string GetGroup(string input)
    {
        string output = string.Empty;
        foreach (char letter in input)
            for (int i = 0; i < 4; i++)
                if (groups[i].Contains(letter))
                    output += i;
        return output;
    }

    bool CheckAnswer(string input)
    {
        return GetGroup(input[chosenGroup]) == submitGroup;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} roll] to roll the dice. Use [!{0} submit] to press the submit button.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        string command = input.Trim().ToUpperInvariant();
        if (command == "ROLL")
        {
            yield return null;
            roll.OnInteract();
        }
        if (command == "SUBMIT")
        {
            yield return null;
            submit.OnInteract();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (!CheckAnswer(displayed))
            {
                roll.OnInteract();
                yield return new WaitForSeconds(0.15f);
            }
            submit.OnInteract();
        }
    }
}
