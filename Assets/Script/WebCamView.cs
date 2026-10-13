using UnityEngine;
using UnityEngine.UI;   // RawImage を使うために必要

public class WebCamView : MonoBehaviour
{
    WebCamTexture webCamTexture;   // カメラ映像を入れる箱

    void Start()
    {
        //PCカメラ
        webCamTexture = new WebCamTexture();
        //"RawImage"にカメラ映像を表示
        GetComponent<RawImage>().texture = webCamTexture;
        webCamTexture.Play();
    }
}