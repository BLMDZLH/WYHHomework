using UnityEngine;
using UnityEngine.UI;

public class BVHUIController : MonoBehaviour
{
    public BVHPlayer player;
    public Button playButton;
    public Text playButtonText;
    public Slider speedSlider;
    public Text infoText;

    void Start()
    {
        playButton.onClick.AddListener(() =>
        {
            player.TogglePlay();
            playButtonText.text = player.isPlaying ? "ÔÝÍ£" : "²¥·Å";
        });

        speedSlider.onValueChanged.AddListener(v => player.SetSpeed(v));
    }

    void Update()
    {
        if (player != null && player.Data != null)
        {
            infoText.text = $"Ö¡: {player.CurrentFrame + 1}/{player.Data.frameCount}   ËÙ¶È: {player.playbackSpeed:F2}x";
        }
    }
}