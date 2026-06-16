using TMPro;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private NPCMood npcMood;
    [SerializeField] private SeasonManager seasonManager;

    [Header("NPC Info")]
    [SerializeField] private string npcName = "Maya";

    [Header("Floating UI")]
    [SerializeField] private GameObject npcNameUI;
    [SerializeField] private TMP_Text floatingNameText;
    [SerializeField] private GameObject interactPromptUI;
    [SerializeField] private TMP_Text promptText;

    [Header("Menu UI")]
    [SerializeField] private GameObject npcMenuPanel;
    [SerializeField] private TMP_Text npcMenuNameText;
    [SerializeField] private TMP_Text dialogueText;

    private bool playerInRange;

    private readonly string[] greetings =
    {
        "Chào mừng, nông dân.",
        "Vui mừng được gặp lại cậu.",
        "Cậu đang tìm hạt giống hôm nay?",
        "Đất đai dường như rất sẵn lòng hôm nay."
    };

    private readonly string[] smallTalk =
{
    "Đừng quên tưới nước cho cây trồng mỗi ngày nhé.",
    "Thời tiết hôm nay khá đẹp, rất thích hợp để làm việc ngoài đồng.",
    "Làm việc chăm chỉ là tốt, nhưng cũng đừng quên nghỉ ngơi lấy sức.",
    "Mỗi hạt giống đều mang theo hy vọng về một mùa vụ bội thu.",
    "Tôi thấy cậu chăm sóc nông trại ngày càng tốt đấy.",
    "Nếu thấy quá mệt thì nên nghỉ ngơi. Sức khỏe mới là vốn quý nhất.",
    "Một mùa vụ thành công luôn bắt đầu từ sự kiên nhẫn.",
    "Những người chăm chỉ thường là những người có mùa màng tốt nhất."
};

private readonly string[] socialNews =
{
    "Nghe nói giá rau ngoài chợ đang tăng khá mạnh.",
    "Dân làng đang bàn tán về việc mở rộng khu chợ phía đông.",
    "Người ta nói thương nhân từ thành phố sẽ ghé qua vào cuối tuần.",
    "Có tin đồn giá trái cây sẽ tăng vào cuối mùa.",
    "Chính quyền đang xem xét hỗ trợ các nông dân mới.",
    "Nhiều thương lái đang tìm nguồn cung nông sản ổn định.",
    "Lễ hội mùa màng có thể sẽ được tổ chức sớm hơn năm ngoái.",
    "Mọi người đang khá lạc quan về vụ mùa năm nay."
};

private readonly string[] farmingTips =
{
    "Đừng trồng tất cả tiền của cậu vào một loại cây duy nhất.",
    "Luôn giữ lại một ít hạt giống cho mùa tiếp theo.",
    "Tưới nước đều đặn quan trọng hơn việc bón quá nhiều phân.",
    "Những loại cây có thời gian sinh trưởng ngắn thường quay vòng vốn nhanh hơn.",
    "Nếu có ít tiền, hãy ưu tiên các loại cây chi phí thấp nhưng ổn định.",
    "Đừng quên nâng cấp công cụ khi có cơ hội.",
    "Quan sát giá thị trường trước khi quyết định trồng số lượng lớn.",
    "Đôi khi lợi nhuận cao nhất đến từ việc bán đúng thời điểm."
};

    private void Awake()
    {
        if (npcMood == null)
            npcMood = GetComponent<NPCMood>();

        if (floatingNameText != null)
            floatingNameText.text = npcName;

        if (promptText != null)
            promptText.text = "[F] Interact";

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (npcMenuPanel != null)
            npcMenuPanel.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.F))
            OpenNPCMenu();

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseNPCMenu();
    }

    public void OpenNPCMenu()
    {
        if (npcMood != null && npcMood.IsAngry)
        {
            if (promptText != null)
                promptText.text = "Maya đang cảm thấy khó chịu. Vui lòng quay lại sau";

            return;
        }

        if (npcMenuPanel != null)
            npcMenuPanel.SetActive(true);

        if (npcMenuNameText != null)
            npcMenuNameText.text = npcName;

        if (dialogueText != null)
            dialogueText.text = greetings[Random.Range(0, greetings.Length)];

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    public void CloseNPCMenu()
    {
        if (npcMenuPanel != null)
            npcMenuPanel.SetActive(false);

        if (playerInRange && interactPromptUI != null)
            interactPromptUI.SetActive(true);
    }

    public void AskAboutToday()
{
    if (dialogueText == null) return;

    string[] tinTuc =
    {
        "Nghe nói giá rau ngoài chợ đang tăng khá mạnh. Có lẽ đây là thời điểm tốt để bán nông sản.",
        
        "Dân làng đang bàn tán về việc mở rộng khu chợ phía đông. Nếu thành công, việc buôn bán sẽ nhộn nhịp hơn nhiều.",
        
        "Thời tiết dạo này khá thất thường. Tôi khuyên cậu nên chuẩn bị sẵn vài hạt giống dự phòng.",
        
        "Nghe nói chính quyền đang xem xét hỗ trợ những nông dân mới lập nghiệp.",
        
        "Thương nhân từ thị trấn bên cạnh sẽ ghé qua vào cuối tuần này. Có thể họ sẽ mua nông sản với giá tốt.",
        
        "Mấy ngày gần đây nhiều người chuyển sang trồng khoai tây. Không biết thị trường có bị bão hòa hay không nữa.",
        
        "Người dân đang lo lắng vì giá phân bón có dấu hiệu tăng lên trong thời gian tới.",
        
        "Tôi nghe nói hội nông dân sắp tổ chức một cuộc thi mùa vụ. Người thắng sẽ nhận được phần thưởng khá lớn.",
        
        "Có tin đồn rằng một thương nhân giàu có đang tìm nguồn cung rau củ số lượng lớn.",
        
        "Dạo gần đây lượng khách ghé thị trấn tăng lên đáng kể. Các cửa hàng đều đang kinh doanh rất tốt.",
        
        "Nghe nói mùa này những ai trồng cây ăn quả sẽ có lợi nhuận khá cao.",
        
        "Mọi người đang bàn tán về việc xây thêm kho chứa nông sản gần khu chợ.",
        
        "Một số nông dân cho rằng mùa vụ năm nay sẽ bội thu hơn năm trước.",
        
        "Tôi vừa nghe tin giá trái cây có thể tăng vào cuối mùa.",
        
        "Thị trấn đang cân nhắc tổ chức lễ hội mùa màng trong thời gian tới."
    };

    dialogueText.text =
        tinTuc[Random.Range(0, tinTuc.Length)];

    if (npcMood != null)
        npcMood.IncreaseMood(2f);
}

    public void AskAboutCrops()
{
    if (dialogueText == null) return;

    if (seasonManager == null)
    {
        dialogueText.text =
            "Tôi cũng không rõ bây giờ đang là mùa gì nữa.";
        return;
    }

    switch (seasonManager.currentSeason)
    {
        case SeasonManager.Season.Spring:
            dialogueText.text =
                "Mùa xuân rất thích hợp để trồng Khoai Tây, Củ Cải và Cà Rốt.";
            break;

        case SeasonManager.Season.Summer:
            dialogueText.text =
                "Mùa hè nên ưu tiên Dưa Hấu, Cà Chua và Việt Quất để kiếm lời.";
            break;

        case SeasonManager.Season.Fall:
            dialogueText.text =
                "Mùa thu là thời điểm tuyệt vời để trồng Bí Ngô, Nho và Cà Tím.";
            break;

        case SeasonManager.Season.Winter:
            dialogueText.text =
                "Mùa đông khá khắc nghiệt. Chỉ một số loại hạt giống đặc biệt mới sinh trưởng được.";
            break;
    }

    if (npcMood != null)
        npcMood.IncreaseMood(1f);
}

    public void OpenShopFromMenu()
    {
        CloseNPCMenu();

        if (shopManager != null)
            shopManager.OpenShop(npcMood);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (npcNameUI != null)
            npcNameUI.SetActive(true);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(true);

        if (promptText != null)
            promptText.text = "[F] Interact";
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        CloseNPCMenu();

        if (shopManager != null)
            shopManager.CloseShop();
    }

    public void Talk()
{
    dialogueText.text =
        smallTalk[Random.Range(0, smallTalk.Length)];

    if (npcMood != null)
        npcMood.IncreaseMood(2f);
}
public void AskNews()
{
    dialogueText.text =
        socialNews[Random.Range(0, socialNews.Length)];

    if (npcMood != null)
        npcMood.IncreaseMood(1f);
}

public void AskFarmingTips()
{
    dialogueText.text =
        farmingTips[Random.Range(0, farmingTips.Length)];

    if (npcMood != null)
        npcMood.IncreaseMood(1f);
}
}