﻿namespace Dibix.Worker.Abstractions
{
    public interface IWorkerScopeFactory
    {
        IWorkerScope Create();
    }
}