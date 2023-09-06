namespace Core.Enums
{
    public enum CustomEventType
    {
        BeforeCopied,
        AfterWebsocket,
        AfterCopied,
        BeforeDeleted,
        AfterDeleted,
        Deactivated,
        BeforePasted,
        AfterPasted,
        BeforeCreated,
        AfterCreated,
        BeforeEmptyRowCreated,
        AfterEmptyRowCreated,
        BeforeCreatedList,
        AfterCreatedList,
        Selected,
        RowFocusIn,
        RowFocusOut,
        RowMouseEnter,
        RowMouseLeave,
        OpenRef,
        BeforePatchUpdate,
        ValidatePatchUpdate,
        BeforePatchCreate,
        AfterPatchUpdate,
    }

    public enum TypeEntityAction
    {
        UpdateEntity = 1,
        UpdateCountBadge = 2,
        Message = 3,
        MessageCountBadge = 4,
    }

    public enum OperatorEnum
    {
        In = 1,
        NotIn = 2,
        Gt = 3,
        Ge = 4,
        Lt = 5,
        Le = 6,
        Lr = 7,
        Rl = 8,
    }
}
