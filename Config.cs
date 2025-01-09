using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WelcomeSystem;

public class BaseConfigs : BasePluginConfig
{
    
    [JsonPropertyName("Texts")]
    public List<TextConfig> Texts { get; set; } = new()
    {
        new TextConfig
        {
            MessageText = "Bienvenido al servidor!",
            FontSize = 30,
            Color = "Green",
        },
        new TextConfig
        {
            MessageText = "Presione E para salir",
            FontSize = 20,
            Color = "Red",
        }
    };
}

public class TextConfig
{
    [JsonPropertyName("MessageText")]
    public string MessageText { get; set; } = "Hello World";

    [JsonPropertyName("FontSize")]
    public int FontSize { get; set; } = 20;

    [JsonPropertyName("Color")]
    public string Color { get; set; } = "White";

    [JsonPropertyName("OffsetZ")]
    public float OffsetZ { get; set; } = 5.0f;

    [JsonPropertyName("WorldUnitsPerPx")]
    public float WorldUnitsPerPx { get; set; } = 0.5f;

    [JsonPropertyName("OffsetForward")]
    public float OffsetForward { get; set; } = 50.0f; // adelante

    [JsonPropertyName("OffsetRight")]
    public float OffsetRight { get; set; } = -40.0f; // lateral

}