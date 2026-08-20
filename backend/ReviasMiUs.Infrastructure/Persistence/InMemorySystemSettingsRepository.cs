using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Settings;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemorySystemSettingsRepository(InMemoryErpStore store) : ISystemSettingsRepository
{
    public SystemSettings Get() => store.Settings;
    public void Update(SystemSettings settings) { }
}
