using System;
using System.Collections.Generic;

// 전투 중 일어난 일(턴, 주사위, 카드 사용, 피해/방어/회복)을 한 줄씩 모아 두는 기록.
// 전투 씬의 BattleLogUI가 OnChanged를 구독해 화면에 표시한다. 전투가 시작될 때 TurnManager가 비운다.
public static class BattleLog
{
    public enum Kind
    {
        System,  // 전투 시작/턴 구분/승패 등
        Dice,
        Card,
        Damage,
        Shield,
        Heal,
        HpLoss,
        Warning, // 보스 기믹 위험 알림 (예: 가시 갑옷 반사 피해 주의)
    }

    public struct Entry
    {
        public string Message;
        public Kind Kind;
        public bool Center; // 흐름을 알려주는 줄(전투 시작/차례/승패)은 가운데 정렬
        public int Turn;    // 0보다 크면 "── N턴 ──" 구분선 (UI가 로그 창 너비에 맞춰 선을 채움)
    }

    // 한 전투에서 이보다 길어지면 오래된 줄부터 지움
    public const int MaxLines = 200;

    private static readonly List<Entry> entries = new List<Entry>();
    public static IReadOnlyList<Entry> Entries => entries;

    public static event Action OnChanged;

    public static void Clear()
    {
        entries.Clear();
        OnChanged?.Invoke();
    }

    public static void Add(string message, Kind kind = Kind.System)
    {
        AddEntry(new Entry { Message = message, Kind = kind });
    }

    public static void AddHeader(string message)
    {
        AddEntry(new Entry { Message = message, Kind = Kind.System, Center = true });
    }

    // 위험 알림: 눈에 띄게 가운데 정렬 + 경고 색
    public static void AddWarning(string message)
    {
        AddEntry(new Entry { Message = message, Kind = Kind.Warning, Center = true });
    }

    public static void AddTurn(int turn)
    {
        AddEntry(new Entry { Message = $"{turn}턴", Kind = Kind.System, Center = true, Turn = turn });
    }

    private static void AddEntry(Entry entry)
    {
        entries.Add(entry);
        if (entries.Count > MaxLines) entries.RemoveAt(0);
        OnChanged?.Invoke();
    }

    public static string ColorOf(Kind kind)
    {
        switch (kind)
        {
            case Kind.Dice: return "#E6C35C";
            case Kind.Card: return "#FFFFFF";
            case Kind.Damage: return "#FF6B6B";
            case Kind.Shield: return "#6FA8FF";
            case Kind.Heal: return "#6BD66B";
            case Kind.HpLoss: return "#E0904F";
            case Kind.Warning: return "#FF9F1C";
            default: return "#A0A0A0";
        }
    }
}
