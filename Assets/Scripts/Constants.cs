using UnityEngine;
public class Constants
{
    public const float GRAVITY_ACCELERATION = 9.81f;
    public const int TANK_RORATE_LOBBY_SPEED = 5;
    public const int UI_SCROLL_SPEED = 1000;
    public const int TANK_SCROLL_SPEED = 20;
    public const int TANK_TURRET_RORATE_SPEED = 4;
    public const int INVISIBLE_TIME = 5;
    public const int SPAWN_ITEM_INTERVAL = 20;
    public const int PARACHUTE_RORATE_SPEED = 20;
    public const int HOMING_MISSILE_DAMAGE = 40;
    public const int HOMING_MISSILE_LAUCH_FORCE = 50;
    public const int SHIELD_LIFE_TIME = 15;
    public const int HEALTH_RESTORE_AMMOUNT = 30;
    public static readonly Color[] Colors = new Color[] { Color.magenta, Color.red, Color.cyan, Color.blue, Color.green, Color.yellow };

    public const int START_GAME_WAIT_TIME = 10;
    #region Offline
    public const int AI_PATROL_SPEED = 2;
    public const int MAIN_OBJECT_SCORE = 10000;
    public const int BONUS_OBJECT_SCORE = 10000;
    public const int BOOST_TIME = 10;
    public static readonly int[] TANK_COSTS = { 0, 5, 9 };
    public static readonly int[] TANK_DAMAGE = { 50, 75, 100 };
    public static readonly int[] TANK_SPEED = { 5, 6, 4 };
    public static readonly int[] TANK_MULTI_SPEED = { 5, 4 };
    public static readonly int[] TANK_MULTI_DAMAGE = { 50, 100 };
    #endregion


}
