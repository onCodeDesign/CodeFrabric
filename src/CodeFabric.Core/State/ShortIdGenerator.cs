using System.Security.Cryptography;
using System.Text;

namespace CodeFabric.Core.State;

/// <summary>
/// Generates a deterministic, human-friendly 6-character identifier from a
/// workflow instance's <c>issueNumber</c> and <c>workflowId</c>.
/// </summary>
/// <remarks>
/// Algorithm: SHA-256 of <c>"{issueNumber}|{workflowId}"</c> (UTF-8), first 8 bytes
/// interpreted as an unsigned 64-bit big-endian integer, encoded in base-36 (digits
/// <c>0–9</c> then <c>a–z</c>), then left-padded with <c>'0'</c> and truncated to
/// exactly <see cref="Length"/> characters.
/// </remarks>
public static class ShortIdGenerator
{
    /// <summary>The fixed length of every generated short ID.</summary>
    public const int Length = 6;

    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Computes a <see cref="Length"/>-character short ID for the given inputs.
    /// </summary>
    /// <param name="issueNumber">The GitHub issue number, e.g. <c>"42"</c>.</param>
    /// <param name="workflowId">The unique workflow run identifier, e.g. <c>"feature-42-20250324-143000"</c>.</param>
    /// <returns>A 6-character lowercase alphanumeric string.</returns>
    public static string Compute(string issueNumber, string workflowId)
    {
        byte[] input = Encoding.UTF8.GetBytes($"{issueNumber}|{workflowId}");
        byte[] hash  = SHA256.HashData(input);

        // Read first 8 bytes as big-endian uint64.
        ulong value = 0;
        for (int i = 0; i < 8; i++)
            value = (value << 8) | hash[i];

        // Encode in base-36.
        Span<char> buffer = stackalloc char[Length];
        for (int i = Length - 1; i >= 0; i--)
        {
            buffer[i] = Alphabet[(int)(value % 36)];
            value /= 36;
        }

        return new string(buffer);
    }
}
