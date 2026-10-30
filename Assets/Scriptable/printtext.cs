using UnityEngine;

public class PrintText : MonoBehaviour
{
    // Kéo file "LoiChao" vừa tạo vào đây
    public TextData duLieuText; 

    void Start()
    {
        // Lấy biến noiDung từ file ScriptableObject và in ra Console
        Debug.Log(duLieuText.noiDung);
    }
}