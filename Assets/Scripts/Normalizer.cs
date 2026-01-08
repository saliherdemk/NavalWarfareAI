using UnityEngine;

namespace NormalizerClass
{
    public static class Normalizer
    {
        public const int ENEMY_CONTROLLER_OBS_SIZE = 20;
        public const int ENEMY_PLAYER_OBS_SIZE = 3;
        public const int ENEMY_TARGET_OBS_SIZE = 3;
        public const int ENEMY_REL_VELOCITY_OBS_SIZE = 2;

        public const int ENEMY_OBS_SIZE =
            ENEMY_CONTROLLER_OBS_SIZE
            + ENEMY_PLAYER_OBS_SIZE
            + ENEMY_TARGET_OBS_SIZE
            + ENEMY_REL_VELOCITY_OBS_SIZE;

        public const int PLAYER_CONTROLLER_OBS_SIZE = 38;
        public const int PLAYER_TARGET_OBS_SIZE = 3;

        public const int PLAYER_OBS_SIZE = PLAYER_CONTROLLER_OBS_SIZE + PLAYER_TARGET_OBS_SIZE;

        public static float mineCount = 10;
        public static float mineCooldownTime = 5f;

        public static float mapWidth = 200f;
        public static float mapHeight = 200f;
        public static float minSpeed = 4f;
        public static float maxSpeed = 8f;
        public static float rayDistance = 32f;
        public static float radarRange = 50f;
        public static float minAcceleration = 3f;
        public static float maxAcceleration = 6f;
        public static float minTurnSpeed = 40f;
        public static float maxTurnSpeed = 100f;

        public static void NormalizeEnemyController(float[] s, float[] destination)
        {
            int index = 0;

            destination[index++] = s[0] / maxSpeed;
            destination[index++] = s[1] / maxSpeed;

            for (int i = 2; i < 2 + 16; i++)
            {
                destination[index++] = s[i] / rayDistance;
            }

            destination[index++] = s[18] / mineCount;
            destination[index++] = s[19] / mineCooldownTime;
        }

        public static void NormalizeRelativePositionData(float[] s, float[] destination)
        {
            int index = 0;
            destination[index++] = s[0];
            destination[index++] = s[1];
            destination[index++] = s[2] / 283f; // 200 * sqrt(2) ;
        }

        public static void NormalizeRelativeVelocity(float[] s, float[] destination)
        {
            int index = 0;
            destination[index++] = s[0] / maxSpeed;
            destination[index++] = s[1] / maxSpeed;
        }

        public static void NormalizePlayerController(float[] s, float[] destination)
        {
            int index = 0;

            destination[index++] = s[0] / maxSpeed;
            destination[index++] = s[1] / maxSpeed;

            for (int i = 2; i < 2 + 16; i++)
            {
                destination[index++] = s[i] / rayDistance;
            }

            destination[index++] = s[18];
            destination[index++] = s[19] / radarRange;
            destination[index++] = s[20];
            destination[index++] = s[21];
            destination[index++] = s[22] / maxSpeed;

            destination[index++] = s[23];
            destination[index++] = s[24] / radarRange;
            destination[index++] = s[25];
            destination[index++] = s[26];
            destination[index++] = s[27] / maxSpeed;

            destination[index++] = s[28];
            destination[index++] = s[29] / radarRange;
            destination[index++] = s[30];
            destination[index++] = s[31];
            destination[index++] = s[32];

            destination[index++] = s[33];
            destination[index++] = s[34] / radarRange;
            destination[index++] = s[35];
            destination[index++] = s[36];
            destination[index++] = s[37];
        }
    }
}
