using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialouge
{
    public List<string> openingCutscene;

    public List<string> goodEnding;
    public List<string> neutralEnding;
    public List<string> badEnding;

    public Dictionary<string, DialougeWithCorrectAnswer> questionsAndAnswersForNPC;
    public Dictionary<string, string> npcIntroductionLines;
    private static Dialouge _instance;
    public static Dialouge Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Dialouge();
            }
            return _instance;
        }
    }
    public Dialouge()
    {
        openingCutscene = new List<string>
        {
            "The Keep of Aethelred is lost to an eternal night. The protective Seal is broken.",
            "Monsters swarm the halls. Your ancestor swore an oath to face them. You mostly signed up for the job title.",
            "You are The Gate Keeper. The kingdom's fate depends on your Will, Focus, and Energy.",
            "Your actions are the magic. Use them to mend the Seal and dispel the darkness.",
            "Choose wisely: TALK to save the corrupted. FIGHT to destroy the malicious.",
            "Get to work, Gate Keeper. You are not paid enough for this."
        };


        goodEnding = new List<string>();
        neutralEnding = new List<string>();
        badEnding = new List<string>();
        questionsAndAnswersForNPC = new Dictionary<string, DialougeWithCorrectAnswer>();

        questionsAndAnswersForNPC.Add("The King", new DialougeWithCorrectAnswer(
            "The darkness is absolute. Why struggle, Gate Keeper? What light is worth the pain?",
            "The light we earn is the only one that lasts. We keep fighting.",
            "We shouldn't. I'm ready to quit too, honestly."
        ));

        questionsAndAnswersForNPC.Add("The Doctor", new DialougeWithCorrectAnswer(
            "If the world is sick, is it not kinder to simply end the pain? What is the use of healing a doomed soul?",
            "The greatest kindness is to tend the wound. Hope is the only cure.",
            "If the wound is deep, let it bleed out. I'm tired."
        ));

        questionsAndAnswersForNPC.Add("The Wizard", new DialougeWithCorrectAnswer(
            "This chaos has no pattern. If our reality is broken, how can we possibly trust what we see?",
            "We trust the pattern of our will. The one thing unbroken is our resolve.",
            "You're right. I can't trust my eyes. I should probably go to bed."
        ));

        questionsAndAnswersForNPC.Add("Tired Butler", new DialougeWithCorrectAnswer(
            "I am too tired to polish the silver. Too tired to move. Tell me truly, why should any of us try to get up tomorrow?",
            "We get up because the job isn't done. The castle still needs its keeper.",
            "We shouldn't. I'm starting to think a nap in the gloom is the right choice."
        ));

        questionsAndAnswersForNPC.Add("Demon King", new DialougeWithCorrectAnswer(
            "You are nothing but a bug fighting destiny. Your end is written.",
            "Please don't be mean.",
            "That's not fair."
        ));

        questionsAndAnswersForNPC.Add("The Thief", new DialougeWithCorrectAnswer(
            "I'm only here for the King's jewels. You don't have to get hurt. Just let me pass, idiot.",
            "Maybe I should let you pass.",
            "Stealing is very rude."
        ));

        questionsAndAnswersForNPC.Add("Pied Piper", new DialougeWithCorrectAnswer(
            "I've told the others you're to blame for the night. They all believed me, of course.",
            "No one likes a liar.",
            "That's not good for my reputation."
        ));

        questionsAndAnswersForNPC.Add("Normal Dude with a Bomb", new DialougeWithCorrectAnswer(
            "I am going to hurt you, and it's going to be loud.",
            "Please don't.",
            "That is not good."
        ));

        npcIntroductionLines = new Dictionary<string, string>();
        npcIntroductionLines.Add("The King", "The King stands, wrapped in spectral robes, weeping silently. He eyes you with a mix of despair and fading hope.");
        npcIntroductionLines.Add("The Doctor", "You encounter the Doctor stumbling in the gloom, clutching a broken syringe. Their face is pale with a crushing sickness.");
        npcIntroductionLines.Add("The Wizard", "The Wizard is floating upside down near a collapsed bookshelf, frantically muttering fragmented formulas to himself.");
        npcIntroductionLines.Add("Tired Butler", "The Butler stands motionless, leaning heavily on a spectral towel, looking utterly defeated by sheer exhaustion.");
        
        npcIntroductionLines.Add("Demon King", "A towering shadow, the Demon King laughs—a sound that scrapes against the stone—and radiates pure, cold power.");
        npcIntroductionLines.Add("The Thief", "A hooded sprite materializes from the shadow of a staircase, mocking you with a greedy grin.");
        npcIntroductionLines.Add("Pied Piper", "The Pied Piper stands playing a horrible, dissonant tune that makes the shadow rats surrounding him squirm with unnatural glee.");
        npcIntroductionLines.Add("Normal Dude with a Bomb", "A normal-looking guy stands in the hallway, nervously adjusting the grip on a massive, flashing bomb.");
    }

    public List<string> GetDialogueByEnding(string ending)
    {
        return ending.ToLower() switch
        {
            "good" => goodEnding,
            "neutral" => neutralEnding,
            "bad" => badEnding,
            _ => neutralEnding,
        };
    }
}

public class DialougeWithCorrectAnswer
{
    public string question;
    public string correctAnswer;
    public string wrongAnswer;

    public DialougeWithCorrectAnswer(string question, string correctAnswer, string wrongAnswer)
    {
        this.question = question;
        this.correctAnswer = correctAnswer;
        this.wrongAnswer = wrongAnswer;
    }

    public bool IsCorrect(string answer)
    {
        return answer.Trim().ToLower() == correctAnswer.Trim().ToLower();
    }
}
