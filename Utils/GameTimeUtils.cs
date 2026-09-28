// Utils/GameTimeUtils.cs
using Game.Simulation;

namespace ElectricCarSubsidy.Utils
{
    public static class GameTimeUtils
    {
        // Jour de jeu continu, utilisable pour des comparaisons/additions simples
        // (ex: currentDay + 7f pour un cycle de 7 jours)
        public static float GetCurrentGameDay(TimeSystem timeSystem)
        {
            int daysPerYear = timeSystem.daysPerYear;
            // normalizedDate est la fraction de l'année écoulée (0.0 à 1.0)
            float dayOfYear = timeSystem.normalizedDate * daysPerYear;
            // On ancre sur l'année de jeu pour avoir un compteur qui ne "boucle" jamais
            return (timeSystem.year * daysPerYear) + dayOfYear;
        }
    }
}