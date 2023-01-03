namespace Countdown.ViewModels;

/// <summary>
/// A list item used in the conundrum result ui list
/// Unlike the other lists it has two columns so separate property
/// getters are required for the solution and conundrum words
/// </summary>
internal sealed record ConundrumItem(string Conundrum, string Solution);
