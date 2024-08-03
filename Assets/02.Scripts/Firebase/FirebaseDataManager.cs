using System;
using System.Text;
using EnumTypes;
using EventLibrary;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.Events;

public class FirebaseDataManager : Singleton<FirebaseDataManager>
{
    private DatabaseManager databaseManager;
    private LocalFileManager _localFileManager;
    private Logger _logger;

    protected override void Awake()
    {
        base.Awake();

        databaseManager = DatabaseManager.Instance;
        _localFileManager = LocalFileManager.Instance;

        EventManager<FirebaseEvents>.StartListening(FirebaseEvents.FirebaseInitialized, OnFirebaseInitialized);
        EventManager<FirebaseEvents>.StartListening(FirebaseEvents.FirebaseSignIn, OnFirebaseSignIn);

        // HeroCollectionUpdated 이벤트 리스너 등록
        UnityAction<HeroCollection> onHeroCollectionUpdated = OnHeroCollectionUpdated;
        EventManager<DataEvents>.StartListening(DataEvents.HeroCollectionUpdated, onHeroCollectionUpdated);

        _logger = Logger.Instance;
    }

    private void OnDestroy()
    {
        EventManager<FirebaseEvents>.StopListening(FirebaseEvents.FirebaseInitialized, OnFirebaseInitialized);
        EventManager<FirebaseEvents>.StopListening(FirebaseEvents.FirebaseSignIn, OnFirebaseSignIn);

        // HeroCollectionUpdated 이벤트 리스너 해제
        UnityAction<HeroCollection> onHeroCollectionUpdated = OnHeroCollectionUpdated;
        EventManager<DataEvents>.StopListening(DataEvents.HeroCollectionUpdated, onHeroCollectionUpdated);
    }

    private void OnFirebaseInitialized()
    {
        _logger = Logger.Instance; // Logger 인스턴스 초기화
        
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            _logger.Log($"Realtime Database: {databaseManager}");
            _logger.Log($"Auth: {AuthManager.Instance}");
        });
        
        EventManager<FirebaseEvents>.TriggerEvent(FirebaseEvents.FirebaseDatabaseInitialized);
    }

    private void OnFirebaseSignIn()
    {
        var currentUser = AuthManager.Instance.GetCurrentUser();
        SaveUserData(currentUser);
        databaseManager.LoadHeroDataFromFirebase(heroData =>
        {
            // Firebase에서 불러온 데이터를 GachaManager에 전달합니다.
            GachaManager.Instance.SetHeroData(heroData);
        });
    }

    private async void SaveUserData(FirebaseUser user)
    {
        var userId = user.UserId;

        var userData = await databaseManager.LoadUserData(userId);
        var userHeroCollection = await databaseManager.LoadHeroCollection(userId);

        if (userData == null || userHeroCollection == null)
        {
            HeroCollectionManager.Instance.Initialize(30);
            userData = new UserData(user.UserId, user.DisplayName ?? "None", 1, "None", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            userHeroCollection = new UserHeroCollection(user.UserId, HeroCollectionManager.Instance.ToBase64());

            await databaseManager.SaveUserData(userData);
            await databaseManager.SaveHeroCollection(userHeroCollection);
        }
        else
        {
            UnityMainThreadDispatcher.Enqueue(() => _logger.Log("기존 사용자 데이터가 존재합니다."));
        }
    }

    private void OnHeroCollectionUpdated(HeroCollection heroCollection)
    {
        string userId = AuthManager.Instance.GetCurrentUser().UserId;
        string base64HeroCollection = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonUtility.ToJson(heroCollection)));

        databaseManager.SaveHeroCollection(new UserHeroCollection(userId, base64HeroCollection));
        _localFileManager.SaveHeroCollectionToLocalFile(base64HeroCollection);
    }
}
