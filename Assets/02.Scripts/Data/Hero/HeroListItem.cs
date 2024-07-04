using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroListItem : InfiniteScrollItem
{
    public Image portraitImage; // 영웅 초상화 이미지
    public Image typeImage; // 타입 아이콘 이미지
    public Image rankImage; // 캐릭터 등급에 따른 테두리 이미지
    public GameObject assignedIndicator; // 편성된 영웅 표시 이미지
    public GameObject ownedFilter; // 보유하지 않은 영웅 필터
    public GameObject selectUI; // 선택된 영웅 UI
    public TMP_Text nameText; // 영웅 이름 텍스트
    public TMP_Text levelText; // 영웅 레벨 텍스트

    public GameObject[] starIcons; // 별 갯수를 표시하는 아이콘 배열
    public Sprite[] rankSprites; // 랭크 스프라이트 배열
    public Sprite[] typeSprites; // 타입 스프라이트 배열

    private Button _button;
    private int _originalSiblingIndex; // 원래 자식 인덱스를 저장하기 위한 변수
    private bool _isMovedToLast; // 현재 오브젝트가 최하위 자식으로 이동되었는지 여부를 저장하기 위한 변수
    private bool _isOwned;

    private void Awake()
    {
        _button = gameObject.GetComponent<Button>();
        _button.onClick.AddListener(OnClickHeroListItem);
        _originalSiblingIndex = transform.GetSiblingIndex(); // 초기화 시 원래 인덱스를 저장
        _isMovedToLast = false; // 초기 상태를 false로 설정
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClickHeroListItem);
    }

    public override void UpdateData(InfiniteScrollData scrollData)
    {
        HeroData heroData = scrollData as HeroData;
        if (heroData == null) return;

        // 텍스트와 이미지 업데이트
        nameText.text = heroData.name;
        portraitImage.sprite = Resources.Load<Sprite>(heroData.portraitPath);
        typeImage.sprite = GetTypeSprite(heroData.type);
        levelText.text = $"Lv.{heroData.level}";

        // 별 아이콘 업데이트
        for (int i = 0; i < starIcons.Length; i++)
        {
            starIcons[i].SetActive(i < heroData.starLevel);
        }

        // 랭크 이미지 업데이트
        rankImage.sprite = GetRankSprite(heroData.rank);

        // 편성 상태 업데이트
        assignedIndicator.SetActive(HeroCollectionManager.Instance.IsHeroAssigned(heroData.id));

        // 보유 상태 업데이트
        _isOwned = HeroCollectionManager.Instance.HasHero(heroData.id);
        
        // 보유하지 않은 영웅일 경우 어두운 필터 적용
        ownedFilter.SetActive(!_isOwned);
    }

    private Sprite GetRankSprite(string grade)
    {
        return grade switch
        {
            "S" => rankSprites.Length > 0 ? rankSprites[0] : null,
            "A" => rankSprites.Length > 1 ? rankSprites[1] : null,
            "B" => rankSprites.Length > 2 ? rankSprites[2] : null,
            "C" => rankSprites.Length > 3 ? rankSprites[3] : null,
            "D" => rankSprites.Length > 4 ? rankSprites[4] : null,
            _ => null
        };
    }

    private Sprite GetTypeSprite(string type)
    {
        return type switch
        {
            "근거리형" => typeSprites.Length > 0 ? typeSprites[0] : null,
            "원거리형" => typeSprites.Length > 1 ? typeSprites[1] : null,
            "방어형" => typeSprites.Length > 2 ? typeSprites[2] : null,
            "만능형" => typeSprites.Length > 3 ? typeSprites[3] : null,
            _ => null
        };
    }

    public void OnClickHeroListItem()
    {
        if (!_isOwned) return; // 영웅이 보유되지 않은 경우 클릭 이벤트 무시
        
        // 현재 비활성화 상태를 반영하여 Manager에 등록
        if (!selectUI.activeSelf)
        {
            HeroListItemManager.Instance.RegisterSelection(this);
        }
        else
        {
            HeroListItemManager.Instance.ClearCurrentSelection();
        }
        
        // 선택 UI의 활성화 상태 토글
        bool isActive = selectUI.activeSelf;
        selectUI.SetActive(!isActive);

        // 현재 오브젝트가 최하위 자식으로 이동되었는지 여부에 따라 처리
        if (_isMovedToLast)
        {
            // 원래 자식 인덱스로 이동
            transform.SetSiblingIndex(_originalSiblingIndex);
        }
        else
        {
            // 현재 자식 인덱스를 저장하고 최하위 자식으로 이동
            _originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
        }

        // 상태 토글
        _isMovedToLast = !_isMovedToLast;
    }
}
