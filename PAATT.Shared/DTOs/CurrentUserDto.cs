namespace PAATT.Shared.DTOs;

public sealed record CurrentUserDto(string Id, string Name, IReadOnlyList<string> Roles);
