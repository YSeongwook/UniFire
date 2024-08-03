using System;
using EnumTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventLibrary;
using Firebase.Auth;
using Gpm.Ui;
using Newtonsoft.Json;

namespace Chat
{
    public class ChatUIController : MonoBehaviour
    {
        public InfiniteScroll chatScroll;
        public TMP_InputField chatInputField;
        public Button sendButton;

        private Logger _logger;
        private ScrollRect _scrollRect;
        private FirebaseUser _currentUser; // 현재 로그인한 사용자
        private TcpClientManager _tcpClientManager;

        [SerializeField] private string serverIp = "127.0.0.1"; // 기본값을 로컬호스트로 설정
        [SerializeField] private int port = 8080;

        private void Awake()
        {
            EventManager<FirebaseEvents>.StartListening(FirebaseEvents.FirebaseSignIn, OnFirebaseSignIn);
        }

        private void OnEnable()
        {
            _scrollRect = chatScroll.GetComponent<ScrollRect>();
            sendButton.onClick.AddListener(OnSendButtonClicked);
            _logger = Logger.Instance;

            _tcpClientManager = new TcpClientManager(serverIp, port);
            _tcpClientManager.OnMessageReceived += HandleMessageReceived;
            DebugLogger.Log("Chat UI 활성화됨 - 서버에 연결");

            OnFirebaseSignIn();
        }

        private void OnDisable()
        {
            _tcpClientManager?.Disconnect();
            DebugLogger.Log("Chat UI 비활성화됨 - 서버 연결 해제");
        }

        private void OnDestroy()
        {
            EventManager<FirebaseEvents>.StopListening(FirebaseEvents.FirebaseSignIn, OnFirebaseSignIn);
        }

        private void OnFirebaseSignIn()
        {
            _currentUser = AuthManager.Instance.GetCurrentUser();
            DebugLogger.Log($"사용자 {_currentUser.DisplayName}로 로그인됨");
        }

        private void OnSendButtonClicked()
        {
            // 버튼 클릭 시 호출
            _logger.Log("버튼 클릭함");
            string message = chatInputField.text;
            if (!string.IsNullOrEmpty(message))
            {
                // ChatMessageData 객체 생성 및 초기화
                var chatMessage = new ChatMessageData
                {
                    userName = _currentUser?.DisplayName,
                    message = message,
                    Timestamp = DateTime.Now,
                    userAvatar = LoadUserAvatar(_currentUser?.DisplayName) // 리소스 폴더에서 아바타 로드
                };

                // 메시지를 JSON 형식으로 직렬화 (userAvatar 제외)
                string jsonMessage = JsonConvert.SerializeObject(chatMessage);
                _tcpClientManager.SendMessageToServer(jsonMessage);
                _logger.Log("보낸 메시지: " + jsonMessage);
                chatInputField.text = string.Empty;
                UpdateChatDisplay(chatMessage);
                ScrollToBottom();
            }
        }

        private void HandleMessageReceived(string jsonMessage)
        {
            var chatMessage = JsonConvert.DeserializeObject<ChatMessageData>(jsonMessage);
            _logger.Log("받은 메시지: " + jsonMessage);
            UnityMainThreadDispatcher.Enqueue(() => UpdateChatDisplay(chatMessage));
        }

        private void UpdateChatDisplay(ChatMessageData chatMessage)
        {
            chatScroll.InsertData(chatMessage);
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f; // 0이면 가장 아래로 스크롤
        }
        
        // 사용자 아바타를 리소스 폴더에서 로드하는 메서드
        private Sprite LoadUserAvatar(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            // Resource 폴더의 UserPortraits 폴더에서 아바타 스프라이트 로드
            return Resources.Load<Sprite>($"UserPortraits/{userName}");
        }
    }
}
