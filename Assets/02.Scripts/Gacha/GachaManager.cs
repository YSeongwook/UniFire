using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EnumTypes;
using EventLibrary;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class GachaManager : MonoBehaviour
{
    private const string GachaUrl = "https://asia-northeast1-unifire-ebcc1.cloudfunctions.net/gacha"; // 가챠 API의 URL
    public SummonResultManager summonResultManager; // SummonResultManager 컴포넌트 참조
    private Dictionary<int, HeroData> _heroDataDict;
    private static GachaManager _instance;

    public static GachaManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GachaManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Firebase 및 GPGS 로그인 완료 이벤트 리스너 등록
        EventManager<FirebaseEvents>.StartListening(FirebaseEvents.FirebaseSignIn, LoadHeroData);
    }

    public void LoadHeroData()
    {
        // HeroData를 Firebase에서 불러오도록 수정
        DatabaseManager.Instance.LoadHeroDataFromFirebase(data => {
            _heroDataDict = new Dictionary<int, HeroData>();
            foreach (var hero in data.data)
            {
                _heroDataDict[hero.id] = hero;
            }
        });
    }

    private void OnEnable()
    {
        EventManager<GachaEvents>.StartListening(GachaEvents.GachaSingle, () => PerformGacha(1));
        EventManager<GachaEvents>.StartListening(GachaEvents.GachaTen, () => PerformGacha(10));
        EventManager<GachaEvents>.StartListening(GachaEvents.GachaThirty, () => PerformGacha(30));
        EventManager<GachaEvents>.StartListening(GachaEvents.AddGachaTen, () => PerformGacha(10));
        EventManager<GachaEvents>.StartListening(GachaEvents.AddGachaThirty, () => PerformGacha(30));
    }

    private void OnDisable()
    {
        EventManager<GachaEvents>.StopListening(GachaEvents.GachaSingle, () => PerformGacha(1));
        EventManager<GachaEvents>.StopListening(GachaEvents.GachaTen, () => PerformGacha(10));
        EventManager<GachaEvents>.StopListening(GachaEvents.GachaThirty, () => PerformGacha(30));
        EventManager<GachaEvents>.StopListening(GachaEvents.AddGachaTen, () => PerformGacha(10));
        EventManager<GachaEvents>.StopListening(GachaEvents.AddGachaThirty, () => PerformGacha(30));
    }

    public void SetHeroData(HeroDataWrapper heroData)
    {
        _heroDataDict = new Dictionary<int, HeroData>();
        foreach (var hero in heroData.data)
        {
            _heroDataDict[hero.id] = hero;
        }
    }

    public async void PerformGacha(int drawCount)
    {
        var currentUser = AuthManager.Instance.GetCurrentUser();
        if (currentUser != null)
        {
            string userId = currentUser.UserId; // 현재 사용자 ID 가져오기
            var result = await GachaRequestAsync(userId, drawCount); // 가챠 요청을 비동기로 처리
            if (result != null)
            {
                UpdateUIWithGachaResult(result); // UI 업데이트
                await UpdateHeroCollection(result, userId); // Firebase 데이터 업데이트 및 HeroCollection 수정
            }
        }
        else
        {
            DebugLogger.LogError("유저가 로그인되어 있지 않습니다.");
        }
    }

    private async Task<int[]> GachaRequestAsync(string userId, int drawCount)
    {
        var json = JsonConvert.SerializeObject(new { userId = userId, drawCount = drawCount }); // 요청 데이터를 JSON으로 직렬화
        using (var request = new UnityWebRequest(GachaUrl, "POST")) // POST 요청 생성 및 using 구문 사용
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json); // 요청 바디 설정
            request.uploadHandler = new UploadHandlerRaw(bodyRaw); // 업로드 핸들러 설정
            request.downloadHandler = new DownloadHandlerBuffer(); // 다운로드 핸들러 설정
            request.SetRequestHeader("Content-Type", "application/json"); // 요청 헤더 설정

            var operation = request.SendWebRequest(); // 요청 전송
            while (!operation.isDone)
            {
                await Task.Yield(); // 비동기 대기
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = JsonConvert.DeserializeObject<GachaResult>(request.downloadHandler.text); // 응답 데이터 파싱
                return result.result;
            }
            else
            {
                DebugLogger.LogError("Gacha request failed: " + request.error); // 에러 처리
                return null;
            }
        }
    }

    private void UpdateUIWithGachaResult(int[] heroIds)
    {
        summonResultManager.ClearSummonResults(); // SummonResult 패널 초기화
        UIManager.Instance.panelSummonResult.SetActive(true); // SummonResult 패널 활성화

        List<SummonResultData> summonResults = new List<SummonResultData>();
        foreach (int heroId in heroIds)
        {
            if (_heroDataDict.TryGetValue(heroId, out HeroData heroData))
            {
                summonResults.Add(new SummonResultData
                {
                    id = heroId,
                    portraitPath = heroData.portraitPath,
                    rank = heroData.rank,
                    count = 1 // 획득한 개수를 1로 설정
                });
            }
            else
            {
                DebugLogger.LogError($"영웅 ID {heroId}에 해당하는 데이터를 찾을 수 없습니다.");
            }
        }

        if (summonResultManager != null)
        {
            summonResultManager.UpdateSummonResults(summonResults);
        }
        else
        {
            DebugLogger.LogError("summonResultManager가 초기화되지 않았습니다.");
        }
    }

    // 이 부분의 업데이트에서 assigned가 전부 false가 되는 것 같다.
    private async Task UpdateHeroCollection(int[] heroIds, string userId)
    {
        await DatabaseManager.Instance.UpdateHeroCollection(userId, heroIds); // Firebase 데이터 업데이트
        string base64HeroCollection = HeroCollectionManager.Instance.ToBase64();
        LocalFileManager.Instance.SaveHeroCollectionToLocalFile(base64HeroCollection); // 로컬 파일 업데이트
    }

    [Serializable]
    public class GachaResult
    {
        public int[] result; // 가챠 결과로 얻은 영웅 ID 배열
    }
}
