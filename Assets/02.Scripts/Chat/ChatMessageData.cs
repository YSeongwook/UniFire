using System;
using Gpm.Ui;
using UnityEngine;
using Newtonsoft.Json;

public class ChatMessageData : InfiniteScrollData
{
    public string userName;
    public string message;
    public long timestamp; // DateTime 대신 long 타입의 Unix 타임스탬프 사용

    [JsonIgnore] // JSON 직렬화에서 제외
    public Sprite userAvatar;

    public DateTime Timestamp
    {
        get
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"); // 한국 표준 시간(KST)로 변환
            return TimeZoneInfo.ConvertTime(dateTimeOffset, kst).DateTime;
        }
        set => timestamp = new DateTimeOffset(value).ToUnixTimeSeconds();
    }
}