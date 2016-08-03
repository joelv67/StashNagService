using System;
using System.Threading;

namespace StashNag
{
    public interface ITaskEngine
    {
        void executeStashNag(String stashUrl, CancellationToken processToken);
    }

    public interface INameMetadata
    {
        String Name { get; }
    }
}
