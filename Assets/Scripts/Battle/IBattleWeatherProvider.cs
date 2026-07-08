/// <summary>
/// Decouples the Pokemon stat calculation from BattleSystem.
/// BattleSystem sets Pokemon.WeatherProvider at battle start/end.
/// This allows Pokemon to apply weather-based ability modifiers
/// without directly referencing the BattleSystem singleton.
/// </summary>
public interface IBattleWeatherProvider {
    /// <summary>
    /// Returns the currently active weather condition, or null if none.
    /// </summary>
    WeatherCondition CurrentWeather { get; }
}
