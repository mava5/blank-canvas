
using UnityEngine;


public static class SharedVar
{
    public static class Game
    {
        public static readonly float LEVEL_WIDTH = 20f;
        public static readonly float LEVEL_DEPTH = 10f;

        /// <summary> How far from the chunk camera is positioned. </summary>
        /// <remarks> [-MAX, 0) </remarks>
        public static readonly float CAMERA_Z_OFFSET = -10f;

        /// <summary> How many units the player has to go up for the entire level to be shifted down by the same amount. </summary>
        public static readonly int VERTICAL_OFFSET_INTERVAL = 500;


        /// <summary> The height at which max difficulty is reached. </summary>
        private static readonly ulong MAX_DIFFICULTY_REACHED_HEIGHT = 2000ul;

        public static float GetDifficulty(ulong height) => Mathf.Clamp01((float)height / MAX_DIFFICULTY_REACHED_HEIGHT);


        /// <summary> How many seconds player has to stay still for the liquid to reach them at the start. </summary>
        private static readonly float START_LIQUID_DELAY = 50f;
        /// <summary> How many seconds player has to stay still for the liquid to reach them at the end. </summary>
        private static readonly float END_LIQUID_DELAY = 30f;

        /// <summary> How many units per second the liquid rises at the start. </summary>
        private static readonly float START_LIQUID_SPEED = 3f;
        /// <summary> How many units per second the liquid rises at the end. </summary>
        private static readonly float END_LIQUID_SPEED = 6f;

        public static float GetLiquidMaxDistance(ulong height) => GetLiquidSpeed(height) * GetLiquidDelay(height);
        public static float GetLiquidDelay(ulong height) => Mathf.Lerp(START_LIQUID_DELAY, END_LIQUID_DELAY, GetDifficulty(height));
        public static float GetLiquidSpeed(ulong height) => Mathf.Lerp(START_LIQUID_SPEED, END_LIQUID_SPEED, GetDifficulty(height));


        /// <summary> Pooled chunk objects are destroyed when they remain unused after this many chunks are generated. </summary>
        public static int MAX_POOLED_CHUNKOBJECT_UNUSED_CYCLES = 10;
    }

    public static class Graphics
    {
        public const int MAX_LIGHT_COLORS = 16;
        public const float MAX_LIGHT_RADIUS = 30;
        public const float MAX_LIGHT_INTENSITY = 2f;
        public const float MAX_LIGHT_DEPTH_OFFSET = 5f;

        public const int MAX_OBJECT_LIGHTS = 64;
    }

    public static class Controls
    {
        /// <summary> How much smoothing is applied to accelerometer value. </summary>
        /// <remarks> [0, 1) : 0 = None, 1 = Value won't update </remarks>
        public static readonly float TILTPHONE_SMOOTHING = 0.02f;

        /// <summary> How wide (relative to screen width) is the match finger dead zone, where horizontal input is lerped down to 0. </summary>
        /// <remarks> [0, 0.5] : 0 = No dead zone -> raw input (1 or -1) is used, 0.5 = Dead zone reaches from edge of screen to the middle </remarks>
        public static readonly float MATCHFINGER_DEADZONE_WIDTH = 0.08f;
    }

    public static class Physics
    {
        /// <summary> The maximum angle a ceiling (wall pointing down) can be to stop the player's jump when they bump into it head-on. </summary>
        /// <remarks> [0, 90) : 0 = No wall is considered a ceiling, 90 = Any wall pointing at all up is a ceiling </remarks>
        public static readonly float MAX_JUMP_STOPPING_CEILING_ANGLE = 40f;
        public static readonly float MAX_CEILING_DOT = -Mathf.Cos(MAX_JUMP_STOPPING_CEILING_ANGLE * Mathf.Deg2Rad);

        // The maximum angle a floor can be until player can no longer slide on it upwards
        public static readonly float MAX_SLIDABLE_FLOOR_ANGLE = 30f;
        public static readonly float MAX_SLIDABLE_FLOOR_DOT = Mathf.Cos(MAX_SLIDABLE_FLOOR_ANGLE * Mathf.Deg2Rad);
    }
}