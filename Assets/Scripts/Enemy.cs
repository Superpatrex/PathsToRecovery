using UnityEngine;
using System.Collections.Generic;

public class EnemiesUtil
{
    public static List<Enemy> enemies = new List<Enemy>
    {
        new Enemy("The King", true),
        new Enemy("The Doctor", true),
        new Enemy("The Wizard", true),
        new Enemy("Tired Butler", true),
        new Enemy("Demon King", false),
        new Enemy("The Thief", false),
        new Enemy("Pied Piper", false),
        new Enemy("Normal Dude with a Bomb", false)
    };

    private static Enemy GetRandomEnemy()
    {
        int index = Random.Range(0, enemies.Count);
        return enemies[index];
    }


    public static Enemy GetRandomEnemyAndRemove()
    {
        Enemy enemy = GetRandomEnemy();
        enemies.Remove(enemy);
        return enemy;
    }
}

public class Enemy
{
    private string enemyName;
    private bool isGood;

    public Enemy(string name, bool good)
    {
        this.enemyName = name;
        this.isGood = good;
    }

    public string GetName()
    {
        return this.enemyName;
    }

    public bool IsGood()
    {
        return this.isGood;
    }
}