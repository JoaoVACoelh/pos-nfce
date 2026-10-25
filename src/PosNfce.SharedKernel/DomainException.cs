namespace PosNfce.SharedKernel;

/// <summary>A business rule was broken. The message is shown to the user, so it is written in Portuguese.</summary>
public class DomainException(string message) : Exception(message);
