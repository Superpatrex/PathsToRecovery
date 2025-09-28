using UnityEngine;
using System.Collections.Generic;

public class EnemiesUtil
{
    public static List<Enemy> enemies = new List<Enemy>
    {
        new Enemy("The King", true, 300),
        new Enemy("The Doctor", true, 50),
        new Enemy("The Wizard", true, 100),
        new Enemy("Tired Butler", true, 50),
        new Enemy("Demon King", false, 500),
        new Enemy("The Thief", false, 100),
        new Enemy("The Jester", false, 50),
        new Enemy("Normal Dude with a Bomb", false, 100)
    };

    private static Enemy GetRandomEnemy()
    {
        int index = Random.Range(0, enemies.Count);
        return enemies[index];
    }

    public static Enemy GetRandomEnemyAndRemove()
    {
        //Enemy enemy = getSpecificEnemy("The Wizard");
        Enemy enemy = GetRandomEnemy();
        enemies.Remove(enemy);
        return enemy;
    }

    public static Enemy getSpecificEnemy(string name)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy.GetName() == name)
            {
                enemies.Remove(enemy);
                return enemy;
            }
        }
        return null;
    }

    public static bool IsEnemyListEmpty()
    {
        return enemies.Count == 0;
    }
}

public class Enemy
{
    private string enemyName;
    private bool isGood;
    private int health;
    private int maxHealth;

    public Enemy(string name, bool good, int health)
    {
        this.enemyName = name;
        this.isGood = good;
        this.health = health;
        this.maxHealth = health;
    }

    public string GetName()
    {
        return this.enemyName;
    }

    public bool IsGood()
    {
        return this.isGood;
    }

    public int GetHealth()
    {
        return this.health;
    }

    public int GetMaxHealth()
    {
        return this.maxHealth;
    }

    public void TakeDamage(int damage)
    {
        this.health -= damage;
        if (this.health < 0)
        {
            this.health = 0;
        }
    }

    public bool IsDefeated()
    {
        return this.health <= 0;
    }

    public bool IsTheKing()
    {
        return this.enemyName == "The King";
    }

    public bool IsDemonKing()
    {
        return this.enemyName == "Demon King";
    }
}