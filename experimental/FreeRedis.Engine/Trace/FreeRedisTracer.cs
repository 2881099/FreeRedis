using Microsoft.AspNetCore.Connections.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class FreeRedisTracer
{

    public static long ForceFlushCount;

    public static long NormalFlushCount;

    public static long SenderLockLootCount;

    public static long CurrentSendTaskCount;

    public static long MaxHanldeBacklogCount;

    private static long _currentWaitHandleBacklogCount;
    public static long CurrentWaitHandleBacklogCount
    {
        get { return _currentWaitHandleBacklogCount; }
        set
        {
            if (value > MaxHanldeBacklogCount)
            {
                MaxHanldeBacklogCount = value;
            }
            _currentWaitHandleBacklogCount = value;
        }
    }

    public static long CurrentCompletedTaskCount;

    public static long WaitBytesCount;

    public static long CircleLockLootCount;

    public static string CurrentStep = string.Empty;

    public static long PreBytesLength;

    public static long CicleCapacityExpandCount;

    public static long CurrentUnreadSpanLength;

    public static long AvailableFlushEscapeCount;
    public static long UnAvailableFlushEscapeCount;
    public static long InnerFlushEscapeCount;

    private static long _currentBacklogTaskCount;

    public static long CurrentBacklogTaskCount
    {
        get { return _currentBacklogTaskCount; }
        set
        {
            if (value > MaxBacklogTaskCount)
            {
                MaxBacklogTaskCount = value;
            }
            _currentBacklogTaskCount = value;
        }
    }

    public static long MaxBacklogTaskCount;

   

    private static CancellationTokenSource _cancellation = default!;


    public static void Stop()
    {
        _cancellation.Cancel();
    }
    public static void Start()
    {
        _cancellation = new();
        Task.Run(() =>
        {
            while (!_cancellation.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(@$"
    --- Run Info: ----------------------------------------------------------------------
    Position:   {CurrentStep}
    Circle Expand:  {$"{CicleCapacityExpandCount,-10}"}
    PreBytes:       {$"{PreBytesLength,-10}"}      Loot Circle:             {$"{CircleLockLootCount,-10}"}
    WaitPackets:    {$"{WaitBytesCount,-10}"}      Loot Send:               {$"{SenderLockLootCount,-10}"}
    Pre BG Task:    {$"{CurrentBacklogTaskCount,-10}"}      Pre Hanlde BG WaitTimes: {$"{CurrentWaitHandleBacklogCount,-10}"}
    Max BG Task:    {$"{MaxBacklogTaskCount,-10}"}      Max Hanlde BG WaitTimes: {$"{MaxHanldeBacklogCount,-10}"}

    Force/Normal Flush:             {ForceFlushCount} / {NormalFlushCount}
    Send/Completed Tasks:           {CurrentSendTaskCount} / {CurrentCompletedTaskCount}
    Ava/UnAva/Inner Escape:         {AvailableFlushEscapeCount} / {UnAvailableFlushEscapeCount} / {InnerFlushEscapeCount}
    ------------------------------------------------------------------------------------");

                Console.WriteLine();
                Console.ResetColor();
                Thread.Sleep(7000);
            }
        });
    }
}

