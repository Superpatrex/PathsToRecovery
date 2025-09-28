using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ImageHolder : MonoBehaviour
{
    public enum State
    {
        NEUTRAL,
        BLOCK,
        ATTACK
    }

    [Header("Assign in Inspector")]
    public List<Sprite> DocSprites = new List<Sprite>();
    public List<Sprite> WizardSprites = new List<Sprite>();
    public List<Sprite> ThiefSprites = new List<Sprite>();
    public List<Sprite> KingSprites = new List<Sprite>();
    public List<Sprite> DemonKingSprites = new List<Sprite>();
    public List<Sprite> ButlerSprites = new List<Sprite>();
    public List<Sprite> PiperSprites = new List<Sprite>();
    public List<Sprite> BombDudeSprites = new List<Sprite>();
    public Image fightSprite;
    public Image talkSprite;

    public Sprite GetSprite(string character, State state)
    {
        var sprites = GetSpritesByName(character);
        if (sprites == null || sprites.Count < 3)
            return null;

        switch (state)
        {
            case State.NEUTRAL:
                return sprites[0];
            case State.BLOCK:
                return sprites[1];
            case State.ATTACK:
                return sprites[2];
            default:
                return null;
        }
    }

    private List<Sprite> GetSpritesByName(string name)
    {
        switch (name)
        {
            case "The Doctor":
                return DocSprites;
            case "The Wizard":
                return WizardSprites;
            case "The Thief":
                return ThiefSprites;
            case "The King":
                return KingSprites;
            case "Demon King":
                return DemonKingSprites;
            case "Tired Butler":
                return ButlerSprites;
            case "Pied Piper":
                return PiperSprites;
            case "Normal Dude with a Bomb":
                return BombDudeSprites;
            default:
                return null;
        }
    }
}
