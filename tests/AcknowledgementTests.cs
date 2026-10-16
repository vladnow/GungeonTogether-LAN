using System;
using GungeonTogether.Lan;
internal static class AcknowledgementTests
{
    private static int count;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); count++; Console.WriteLine("PASS: " + name); }
    public static int Main()
    {
        try {
            InputAcknowledgements timer = new InputAcknowledgements();
            Check(!timer.Observe(1, 20), "unknown input acknowledgement ignored");
            timer.Sent(1, 10); Check(timer.Observe(1, 10.075) && Math.Abs(timer.Milliseconds - 75) < 0.001, "response measured using one local clock");
            Check(!timer.Observe(1, 20), "duplicate acknowledgement cannot inflate response");
            timer.Sent(2, 10.1); Check(!timer.Observe(2, 10), "backward clock rejected");
            Check(timer.Observe(2, 10.2), "fresh acknowledgement accepted");
            timer.Sent(1, 20); Check(!timer.Observe(1, 20.2), "stale sequence ignored");
            timer.Clear(); Check(timer.Milliseconds == 0 && !timer.Observe(2, 30), "level reset removes previous response samples");
            timer.Sent(uint.MaxValue, 30); timer.Observe(uint.MaxValue, 30.1); timer.Sent(0, 31);
            Check(timer.Observe(0, 31.1), "input sequence wraparound measured");
            timer.Sent(1, 40); timer.Sent(513, 41);
            Check(!timer.Observe(1, 42) && timer.Observe(513, 42), "bounded ring cannot match overwritten input");
            Console.WriteLine("ACKNOWLEDGEMENT_TESTS_PASS: " + count); return 0;
        } catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}
