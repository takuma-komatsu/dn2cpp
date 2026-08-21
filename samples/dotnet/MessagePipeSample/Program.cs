using System;
using System.Globalization;
using MessagePipe;
using MessagePipePubSubSubset;
using MessagePipeFilterSubset;

namespace MessagePipeSample;

public static class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        MessagePipePubSubSubset.Program.__GateEntry();
        MessagePipeAsyncSubset.Program.__GateEntry();
        MessagePipeReqRespSubset.Program.__GateEntry();
        MessagePipeFilterSubset.Program.__GateEntry();
    }

    // MessagePipe registers its brokers and handlers as OPEN generic services that the
    // DI container closes by reflection, so no IL in this program names
    // MessageBroker<string> and reachability monomorphizes none of them. These fields
    // are the IL that does; without them the resolves above have nothing to bind to.
#pragma warning disable CS0169, CS0649
    private static class AotPreserveHolder
    {
        public static MessageBroker<string> B1;
        public static MessageBroker<IntMessage> B2;
        public static MessageBroker<FilterMessage> B3;
        public static MessageBroker<string, string> B4;
        public static BufferedMessageBroker<string> B5;
        public static AsyncMessageBroker<string> B6;
        public static AsyncMessageBroker<string, string> B7;
        public static BufferedAsyncMessageBroker<string> B8;
        public static RequestHandler<string, string> B9;
        public static AsyncRequestHandler<string, string> B10;
        public static RequestAllHandler<string, string> B11;
    }
#pragma warning restore CS0169, CS0649
}
