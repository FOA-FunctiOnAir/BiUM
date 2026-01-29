namespace BiUM.Core.Common.Enums;

public enum EntityColumnFeatures
{
    None = 0,

    EntityTranslation = 10,

    SetByContextTenantId = 100,
    SetByContextUserId = 101,
    SetByContextWorkgroupId = 102,
    SetByContextRoleId = 103,

    NotSearchable = 200,

    NotReturnForList = 300
}