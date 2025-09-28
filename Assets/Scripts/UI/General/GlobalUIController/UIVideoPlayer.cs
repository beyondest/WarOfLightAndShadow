using UnityEngine;
using UnityEngine.Video;

namespace SparFlame.UI.General
{
    public class UIVideoPlayer : MonoBehaviour
    {
        public VideoPlayer videoPlayer;

        public void PlayVideo()
        {
            videoPlayer.Play();
        }
        public void StopVideo()
        {
            videoPlayer.Stop();
        }
    }

}