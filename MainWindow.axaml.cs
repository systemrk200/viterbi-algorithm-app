using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ViterbiAlgorithmApp;

public partial class MainWindow : Window
{
    // Словарь: слово -> логарифм вероятности
    private Dictionary<string, double> _wordLogProb = new();
    private int _maxWordLength = 0;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnTrainClick(object sender, RoutedEventArgs e)
    {
        var text = TrainingText.Text ?? "";

        var words = Regex.Matches(text, @"[А-Яа-яЁё]+")
                         .Cast<Match>()
                         .Select(m => m.Value.ToLowerInvariant())
                         .ToList();

        if (words.Count == 0)
        {
            StatusText.Text = "Обучающий текст пуст или не содержит слов.";
            return;
        }

        var counts = new Dictionary<string, int>();
        foreach (var w in words)
            counts[w] = counts.GetValueOrDefault(w) + 1;

        int total = words.Count;
        _wordLogProb = counts.ToDictionary(
            kv => kv.Key,
            kv => Math.Log((double)kv.Value / total)
        );

        _maxWordLength = counts.Keys.Max(w => w.Length);

        StatusText.Text = $"Обучение завершено. Слов в словаре: {_wordLogProb.Count}, " +
                          $"макс. длина слова: {_maxWordLength}.";
    }

    private void OnSegmentClick(object sender, RoutedEventArgs e)
    {
        if (_wordLogProb.Count == 0)
        {
            StatusText.Text = "Сначала обучите модель на тексте.";
            return;
        }

        var raw = SegmentInput.Text ?? "";
        var cleaned = Regex.Replace(raw, @"[^А-Яа-яЁё]", "").ToLowerInvariant();

        if (cleaned.Length == 0)
        {
            SegmentResult.Text = "Введите строку без пробелов.";
            return;
        }

        var result = Segment(cleaned);
        if (result == null)
        {
            SegmentResult.Text = "Не удалось разбить строку - нет подходящих слов в словаре.";
            return;
        }

        SegmentResult.Text = string.Join(" ", result);
    }

    // Витерби по позициям в строке.
    private List<string>? Segment(string s)
    {
        int n = s.Length;
        double NEG_INF = double.NegativeInfinity;
        var dp = new double[n + 1];
        var back = new int[n + 1];

        for (int i = 0; i <= n; i++)
        {
            dp[i] = NEG_INF;
            back[i] = -1;
        }
        dp[0] = 0;

        for (int i = 1; i <= n; i++)
        {
            int start = Math.Max(0, i - _maxWordLength);
            for (int j = start; j < i; j++)
            {
                if (dp[j] == NEG_INF) continue;

                string word = s.Substring(j, i - j);
                if (!_wordLogProb.TryGetValue(word, out double lp)) continue;

                double score = dp[j] + lp;
                if (score > dp[i])
                {
                    dp[i] = score;
                    back[i] = j;
                }
            }
        }

        if (dp[n] == NEG_INF) return null;

        var path = new List<string>();
        int pos = n;
        while (pos > 0)
        {
            int j = back[pos];
            path.Add(s.Substring(j, pos - j));
            pos = j;
        }
        path.Reverse();
        return path;
    }
}