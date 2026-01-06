using UnityEngine;

namespace NormalizerClass
{
    public static class Normalizer
    {
        public const int PLAYER_OBS_SIZE = 41;
        public const int ENEMY_OBS_SIZE = 37;

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

        public static int GetPlayerSensorCount() => PLAYER_OBS_SIZE;

        public static int GetEnemySensorCount() => ENEMY_OBS_SIZE;

        public static int GetTargetAngleCount() => 2;

        public static void NormalizePlayerController(float[] s, float[] destination)
        {
            int index = 0;

            destination[index++] = s[0] / maxSpeed;
            destination[index++] = s[1] / maxSpeed;

            for (int i = 2; i < 2 + 16; i++)
            {
                destination[index++] = s[i] / rayDistance;
            }

            NormalizeTarget(destination, ref index, s, 18);
            NormalizeTarget(destination, ref index, s, 23);
            NormalizeMine(destination, ref index, s, 28);
            NormalizeMine(destination, ref index, s, 31);

            destination[index++] = NormalizeRange(s[34], minSpeed, maxSpeed);
            destination[index++] = NormalizeRange(s[35], minAcceleration, maxAcceleration);
            destination[index++] = NormalizeRange(s[36], minTurnSpeed, maxTurnSpeed);
        }

        public static void NormalizeEnemyController(float[] s, float[] destination)
        {
            int index = 0;

            destination[index++] = s[0] / maxSpeed;
            destination[index++] = s[1] / maxSpeed;

            for (int i = 2; i < 2 + 16; i++)
            {
                destination[index++] = s[i] / rayDistance;
            }

            NormalizeTarget(destination, ref index, s, 18);
            NormalizeMine(destination, ref index, s, 23);
            NormalizeMine(destination, ref index, s, 26);

            destination[index++] = s[29] / mineCount;
            destination[index++] = s[30] / mineCooldownTime;

            destination[index++] = NormalizeRange(s[31], minSpeed, maxSpeed);
            destination[index++] = NormalizeRange(s[32], minAcceleration, maxAcceleration);
            destination[index++] = NormalizeRange(s[33], minTurnSpeed, maxTurnSpeed);
        }

        public static float NormalizeTargetDistance(float d)
        {
            return d / 283f; // 200 * sqrt(2)
        }

        public static void NormalizeTargetAngle(float angleDeg, float[] destination)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            destination[0] = Mathf.Cos(rad);
            destination[1] = Mathf.Sin(rad);
        }

        private static void NormalizeTarget(float[] destination, ref int index, float[] s, int i)
        {
            destination[index++] = s[i] / radarRange;
            AddSinCos(destination, ref index, s[i + 1]);
            destination[index++] = s[i + 2] / maxSpeed;
            destination[index++] = s[i + 3] / maxSpeed;
            destination[index++] = s[i + 4] / maxSpeed;
        }

        private static void NormalizeMine(float[] destination, ref int index, float[] s, int i)
        {
            destination[index++] = s[i] / radarRange;
            AddSinCos(destination, ref index, s[i + 1]);
            destination[index++] = s[i + 2] / maxSpeed;
        }

        private static void AddSinCos(float[] destination, ref int index, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            destination[index++] = Mathf.Cos(rad);
            destination[index++] = Mathf.Sin(rad);
        }

        private static float NormalizeRange(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }
    }
}
