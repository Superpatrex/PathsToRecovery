using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialouge
{
    public List<string> openingCutscene;

    public List<string> goodEnding;
    public List<string> neutralEnding;
    public List<string> badEnding;

    public Dictionary<string, List<DialougeWithCorrectAnswer>> questionsAndAnswersForNPC;
    public Dictionary<string, List<string>> npcIntroductionLines;
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
            "The Village of Aethelred is lost to an eternal night. The protective Seal is broken.",
            "An evil mist surges through cobblestone streets. Your ancestor swore an oath to protect the kingdom. You mostly just signed up for the benefits.",
            "You are The Village Warden. The village's fate depends on your Will, Focus, and Energy.",
            "Your judgement is crucial. Use it keenly to mend the Seal and dispel the darkness.",
            "Choose wisely: TALK to save the corrupted villagers. FIGHT to destroy the malicious nightmares.",
            "Good luck, Warden. They'd better give you a raise."
        };

        goodEnding = new List<string>
        {
            "The final piece of the Seal snaps into place. It pulses once, a brilliant, blinding light.",
            "The oppressive, endless night shatters like glass. The Mists of Disquiet vanish.",
            "A glorious, sun rises over the horizon of Aethelred for the first time in years.",
            "The good people you saved stand restored, watching the dawn. They are safe.",
            "The Keep is quiet. You performed your duty with wisdom and strength.",
            "Day has returned. Your files are in order. And for a moment, you almost feel satisfied."
        };

        neutralEnding = new List<string>
        {
            "The Seal is repaired, but the mended lines flicker with uncertainty. It holds, but it is weak.",
            "The magical night recoils, but no light follows. The sun does not rise. It is simply a dull, eternal twilight.",
            "You survived. The Keep is protected from immediate collapse, but it is deeply scarred by your poor choices.",
            "Too many good souls were lost; too many wicked escaped. The balance is permanently off.",
            "The kingdom endures, locked in a perpetual gray gloom, a reminder of your mixed intentions.",
            "Your contract is automatically renewed. The work never ends. Welcome to the new endless night, Gate Keeper."
        };

        badEnding = new List<string>
        {
            "You failed. The fragments of the Seal turn black and crumble into dust.",
            "The Malicious creatures you allowed to live erupt in triumph. The Demon King's laughter echoes through the halls.",
            "The light of the Keep is replaced by a terrible, angry glow. The village is on fire.",
            "Chaos reigns. You destroyed the good and empowered the wicked. The kingdom is consumed.",
            "Your contract is terminated immediately.",
            "The kingdom is lost to the flames."
        };

        questionsAndAnswersForNPC = new Dictionary<string, List<DialougeWithCorrectAnswer>>();

        // --- Good People (Corrupted Villagers) ---

        // The King (Now a village leader/elder)
        questionsAndAnswersForNPC.Add("The King", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Hope vs. Despair
            new DialougeWithCorrectAnswer(
                "The darkness is absolute, Warden. Why struggle? What light is worth saving these few cobblestone paths?",
                "The light we earn is the only one that lasts. We keep fighting for our home.",
                "We shouldn't. I'm ready to quit too, honestly."
            ),
            // Set 2: Burden of Leadership
            new DialougeWithCorrectAnswer(
                "I failed the village. Tell me, is it not the coward's duty to step aside when he has ruined everything?",
                "A true leader's duty is to lead the repair. Your villagers need your return to hope, not despair.",
                "Yes. You are clearly past the point of fixing this. Stand aside."
            ),
            // Set 3: Value of the Village
            new DialougeWithCorrectAnswer(
                "This whole place is filled with suffering. Is it not easier to let the mists claim what is already broken?",
                "A village's value is in its history and its community. We mend what is broken, always.",
                "Honestly, this village is a dump. Let it fall."
            )
        });

        // The Doctor (Village Healer)
        questionsAndAnswersForNPC.Add("The Doctor", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Healing vs. Futility
            new DialougeWithCorrectAnswer(
                "If the world is sick, is it not kinder to simply end the pain? What is the use of healing a doomed soul?",
                "The greatest kindness is to tend the wound. Hope is the only cure.",
                "If the wound is deep, let it bleed out. I'm tired."
            ),
            // Set 2: Contagion of Fear
            new DialougeWithCorrectAnswer(
                "The fear is a contagion. If I save you, will I not simply spread this sickness of doubt further into the square?",
                "Fear is a choice, not a disease. Your work can give others the courage to choose light.",
                "You should probably quarantine yourself. I'll take it from here."
            )
        });

        // The Wizard (Village Scholar/Alchemist)
        questionsAndAnswersForNPC.Add("The Wizard", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Logic vs. Chaos
            new DialougeWithCorrectAnswer(
                "This chaos has no pattern. If our reality is broken, how can we possibly trust the cobblestones beneath our feet?",
                "We trust the pattern of our will. The one thing unbroken is our resolve.",
                "You're right. I can't trust my eyes. I should probably go to bed."
            ),
            // Set 2: The Illusion of Darkness
            new DialougeWithCorrectAnswer(
                "If the darkness is magic, and magic is illusion, then the sun never existed. Why fight for a lie?",
                "Darkness is the illusion, Wizard. The memory of the sun is the most real thing we possess.",
                "Yeah, maybe you're right. The sun was kind of overrated anyway."
            )
        });

        // Tired Butler (Village Sweeper/Care-taker)
        questionsAndAnswersForNPC.Add("Tired Butler", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Perseverance vs. Exhaustion
            new DialougeWithCorrectAnswer(
                "I am too tired to sweep the walkways. Too tired to move. Tell me truly, why should any of us try to get up tomorrow?",
                "We get up because the job isn't done. The village still needs its warden.",
                "We shouldn't. I'm starting to think a nap in the gloom is the right choice."
            ),
            // Set 2: Value of Small Efforts
            new DialougeWithCorrectAnswer(
                "My life was spent sweeping. Now the world is ending. Did all those small efforts mean nothing?",
                "Every act of duty reinforces the fabric of the village. Your efforts are why this place still stands.",
                "It was just sweeping, Butler. No one cares about a clean street now."
            )
        });

        // --- Bad People (Malicious Nightmares) - Require FIGHT ---

        // Demon King
        questionsAndAnswersForNPC.Add("Demon King", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Power Threat
            new DialougeWithCorrectAnswer(
                "You are nothing but a bug fighting destiny. Your end is written.",
                "Please don't be mean.", 
                "That's not fair.", true
            ),
            // Set 2: Inevitable Failure
            new DialougeWithCorrectAnswer(
                "I control the Mists and the night. Your little candle of hope will be extinguished by my breath alone.",
                "That sounds like a terrible way to go.",
                "Don't you have better things to do than talk to me?", true
            )
        });

        // The Thief
        questionsAndAnswersForNPC.Add("The Thief", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Greed/Temptation
            new DialougeWithCorrectAnswer(
                "I'm only here for the villagers' hidden goods. You don't have to get hurt. Just let me pass, idiot.",
                "Maybe I should let you pass.",
                "Stealing is very rude.", true
            ),
            // Set 2: Temptation
            new DialougeWithCorrectAnswer(
                "I have riches hidden under the cobblestones. I'll split them with you. Turn around, Warden, and we'll both be rich.",
                "I don't need your dirty money.",
                "Fifty-fifty is a bad rate.", true
            )
        });

        // Pied Piper
        questionsAndAnswersForNPC.Add("Pied Piper", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Deception/Mockery
            new DialougeWithCorrectAnswer(
                "I've told the others you're to blame for the night. They all believed me, of course.",
                "No one likes a liar.",
                "That's not good for my reputation.", true
            ),
            // Set 2: Mocking Authority
            new DialougeWithCorrectAnswer(
                "The Elder is an imbecile. The Doctor is a hack. You're an idiot. Why defend people who are so weak?",
                "You shouldn't talk about people like that.",
                "You're not wrong, but I'm still the Warden.", true
            )
        });

        // Normal Dude with a Bomb
        questionsAndAnswersForNPC.Add("Normal Dude with a Bomb", new List<DialougeWithCorrectAnswer>
        {
            // Set 1: Simple Threat
            new DialougeWithCorrectAnswer(
                "I am going to hurt you, and it's going to be loud.",
                "Please don't.",
                "That is not good.", true
            ),
            // Set 2: Unpredictable Violence
            new DialougeWithCorrectAnswer(
                "I don't know why I have this, but I'm going to throw it now. Hope you have good insurance.",
                "I think my insurance is lapsed.",
                "I would prefer you didn't throw that.", true
            )
        });

        npcIntroductionLines = new Dictionary<string, List<string>>();
        npcIntroductionLines.Add("The King", new List<string>
        {
            "The King stands, wrapped in spectral robes, weeping silently in the center of the town square.",
            "The King kneels by a ruined storefront, pointing hopelessly into the endless night. He barely notices your arrival.",
            "You find the King chained to a wooden scaffold by thick, spectral ropes of fear, staring blankly ahead."
        });

        // The Doctor
        npcIntroductionLines.Add("The Doctor", new List<string>
        {
            "You encounter the Doctor stumbling on the cobblestones, clutching a broken syringe. Their face is pale with a crushing sickness.",
            "The Doctor is desperately trying to bandage a splintered well-post, muttering that nothing can be healed.",
            "The Doctor is slumped against a wall, arguing with their own shadow about the futility of medicine."
        });

        // The Wizard
        npcIntroductionLines.Add("The Wizard", new List<string>
        {
            "The Wizard is floating upside down near a collapsed market stall, frantically muttering fragmented formulas to himself.",
            "The Wizard is attempting to cast a light spell, but only managed to conjure a very small, sad mouse.",
            "You find the Wizard huddled in the corner of the town square, drawing complex, nonsensical maps in the dust with a charred finger."
        });

        // Tired Butler
        npcIntroductionLines.Add("Tired Butler", new List<string>
        {
            "The Butler stands motionless near a flickering torch, leaning heavily on a spectral mop, looking utterly defeated by exhaustion.",
            "The Butler is slowly and meticulously trying to clean the grime from a stone fountain with a tear-stained rag.",
            "You find the Butler standing guard over an empty, broken wheelbarrow, shaking his head and sighing deeply."
        });

        // --- Bad People (Malicious Nightmares) ---

        // Demon King
        npcIntroductionLines.Add("Demon King", new List<string>
        {
            "A towering shadow, the Demon King laughs—a sound that scrapes against the cobblestones—and radiates cold power.",
            "The Demon King hovers above the walkway, slowly tearing down a tattered banner with one sharp, indifferent claw.",
            "The Demon King stops his steady, rhythmic pacing the moment you enter, locking you with two glowing red eyes."
        });

        // The Thief
        npcIntroductionLines.Add("The Thief", new List<string>
        {
            "A hooded sprite materializes from the shadow of a torch, mocking you with a greedy grin.",
            "The Thief is attempting to pick the lock on the general store, whistling a cheerful, entirely inappropriate tune.",
            "The Thief is holding up a small, worthless trinket, inspecting it with malicious amusement before spotting you."
        });

        // Pied Piper
        npcIntroductionLines.Add("Pied Piper", new List<string>
        {
            "The Pied Piper stands playing a horrible, dissonant tune that makes the shadow rats swarming the streets squirm with glee.",
            "The Pied Piper bows deeply before you, his flute pressed against his lips, ready to unleash a cacophony of fear.",
            "You hear the faint, sickening sound of the flute before you see the Pied Piper leading a small parade of shadow creatures down the walkway."
        });

        // Normal Dude with a Bomb
        npcIntroductionLines.Add("Normal Dude with a Bomb", new List<string>
        {
            "A normal-looking guy stands in the hallway between two houses, nervously adjusting the grip on a massive, flashing bomb.",
            "The Normal Dude with a Bomb is jogging in place on the cobblestones, looking anxious, the bomb bouncing in his sweaty hand.",
            "The Normal Dude with a Bomb turns the corner, jumps slightly when he sees you, and holds the oversized bomb defensively."
        });

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

    public DialougeWithCorrectAnswer getRandomQuestionAndAnswer(string npcName)
    {
        if (questionsAndAnswersForNPC.ContainsKey(npcName))
        {
            var qaList = questionsAndAnswersForNPC[npcName];
            int index = Random.Range(0, qaList.Count);
            return qaList[index];
        }
        else
        {
            Debug.LogError($"No questions found for NPC: {npcName}");
            return null;
        }
    }

    public string getRandomIntroductionLine(string npcName)
    {
        if (npcIntroductionLines.ContainsKey(npcName))
        {
            var lines = npcIntroductionLines[npcName];
            int index = Random.Range(0, lines.Count);
            return lines[index];
        }
        else
        {
            Debug.LogError($"No introduction lines found for NPC: {npcName}");
            return "The NPC is here.";
        }
    }
}

public class DialougeWithCorrectAnswer
{
    public string question;
    public string correctAnswer;
    public string wrongAnswer;
    public bool hasNoCorrectAnswer = false;

    public DialougeWithCorrectAnswer(string question, string correctAnswer, string wrongAnswer)
    {
        this.question = question;
        this.correctAnswer = correctAnswer;
        this.wrongAnswer = wrongAnswer;
    }

    public DialougeWithCorrectAnswer(string question, string correctAnswer, string wrongAnswer, bool hasNoCorrectAnswer)
    {
        this.question = question;
        this.correctAnswer = correctAnswer;
        this.wrongAnswer = wrongAnswer;
        this.hasNoCorrectAnswer = hasNoCorrectAnswer;
    }

    public bool IsCorrect(string answer)
    {
        return answer.Trim().ToLower() == correctAnswer.Trim().ToLower() && !hasNoCorrectAnswer;
    }
}
