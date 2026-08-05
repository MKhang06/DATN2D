using UnityEngine;
using UnityEngine.SceneManagement;

namespace GreenField.UI
{
    public sealed class GreenFieldMainMenuController : MonoBehaviour
    {
        [Header("Scene sẽ được mở từ menu")]
        [SerializeField] private string farmSceneName = "BaoDemo";

        public void StartNewGame()
        {
            LoadBaoDemo();
        }

        public void ContinueGame()
        {
            LoadBaoDemo();
        }

        public void LoadGame()
        {
            LoadBaoDemo();
        }

        public void OpenSettings()
        {
            Debug.Log("[Green Field] Chưa tạo panel Cài đặt.", this);
        }

        public void ReloadCurrentScene()
        {
            Scene currentScene = gameObject.scene;
            SceneManager.LoadScene(currentScene.name, LoadSceneMode.Single);
        }

        public void ExitGame()
        {
            Debug.Log("[Green Field] Thoát game.", this);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadBaoDemo()
        {
            if (string.IsNullOrWhiteSpace(farmSceneName))
            {
                Debug.LogError("[Green Field] Farm Scene Name đang để trống.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(farmSceneName))
            {
                Debug.LogError(
                    "[Green Field] Không thể load scene '" + farmSceneName + "'. " +
                    "Hãy chạy Tools > Green Field > Build Main Menu để thêm BaoDemo vào Build Settings."
                );
                return;
            }

            SceneManager.LoadScene(farmSceneName, LoadSceneMode.Single);
        }
    }
}
