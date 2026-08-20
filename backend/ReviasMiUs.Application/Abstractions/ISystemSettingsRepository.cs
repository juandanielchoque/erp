using ReviasMiUs.Domain.Settings;

namespace ReviasMiUs.Application.Abstractions;

public interface ISystemSettingsRepository
{
    SystemSettings Get();
    void Update(SystemSettings settings);
}
