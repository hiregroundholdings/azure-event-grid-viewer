namespace Viewer
{
    using System;

    public sealed class DuplicateResourceException : Exception
    {
        public DuplicateResourceException(string resourceType, string resourceIdentifier, Exception? innerException = null)
            : base($"The resource {resourceType} '{resourceIdentifier}' already exists.", innerException) { }
    }
}
