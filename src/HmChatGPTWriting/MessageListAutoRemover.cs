﻿using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HmNetCOM;

namespace HmOpenAIChatGptWriting;

partial class OpenAIChatMain
{
    static private readonly object lockObject = new object();

    // 最後の回答があってから5分以上経過したらメッセージリストの一番最初のリストを削除する
    static private DateTime lastAnswerTime = DateTime.Now;
    static private DateTime lastDeleteTime = DateTime.MinValue;

    static CancellationTokenSource? autoRemoverCancelTokenSource;

    static Task taskRunMessageListRemove;
    public static void InitMessageListCancelToken()
    {
        // Hm.OutputPane.Output("InitMessageListCancelToken\r\n");
        lastAnswerTime = DateTime.Now;
        lastDeleteTime = DateTime.Now;

        if (taskRunMessageListRemove == null || taskRunMessageListRemove.IsCompleted)
        {

            /*
            if (taskRunMessageListRemove != null && taskRunMessageListRemove.IsCompleted)
            {
                Hm.OutputPane.Output("タスクはコンプリート\r\n");
            }
            */
            autoRemoverCancelTokenSource = new CancellationTokenSource();

            taskRunMessageListRemove = Task.Run(async () =>
            {
                await RunMessageListRemoveTask();
            });
        }
    }

    public static void CancelMessageListCancelToken()
    {
        autoRemoverCancelTokenSource?.Cancel();
    }


    public static async Task RunMessageListRemoveTask()
    {
        while (true)
        {
            // Hm.OutputPane.Output("RunMessageListRemoveTask");
            if (autoRemoverCancelTokenSource != null && autoRemoverCancelTokenSource.IsCancellationRequested)
            {
                return;
            }

            var lastCondition = (DateTime.Now - lastAnswerTime).TotalMinutes >= iRemoveAutoMessageListMinutes;
            var tickConsition = (DateTime.Now - lastDeleteTime).TotalMinutes >= 1;

            // ５分以上経過していて、1分チックも達成している。
            if (lastCondition && tickConsition)
            {
                RemoveEarliestQandA();
            }
            // 最後の回答があってから５分間以上経過している
            else if (lastCondition)
            {
                RemoveEarliestQandA();
            }

            if (autoRemoverCancelTokenSource != null)
            {
                var token = autoRemoverCancelTokenSource.Token;
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
        }
    }

    public void AddQuestion(string question)
    {
        lock (lockObject)
        {
            messageList.Add(ChatMessage.FromUser(question));
        }
    }
    private static void AddAnswer(string answer_sum)
    {
        lock (lockObject)
        {
            // 今回の返答ををChatGPTの返答として記録しておく
            messageList.Add(ChatMessage.FromAssistant(answer_sum));
            lastAnswerTime = DateTime.Now;
        }
    }

    // メッセージリストの中から一番過去の２つを削除する。
    public static void RemoveEarliestQandA()
    {
        lock (lockObject)
        {
            // 先頭の一つはシステムメッセージなので、index[1]とindex[2]を削除する。
            if (messageList.Count >= 3)
            {
                messageList.RemoveRange(1, 2);
            }
            lastDeleteTime = DateTime.Now;

            // Hm.OutputPane.Output("RemoveEarliestQandA");
        }
    }

}
