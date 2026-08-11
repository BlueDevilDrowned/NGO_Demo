public readonly struct MovementControlRequest
{
    public readonly bool DisableMovement;
    public readonly bool DisableCharacterController;

    public MovementControlRequest(
        bool disableMovement,
        bool disableCharacterController)
    {
        DisableMovement=disableMovement;
        DisableCharacterController=disableCharacterController;
    }

    public static MovementControlRequest DisableAll=>new(true,true);
    public static MovementControlRequest CharacterControllerOnly=>new(false,true);
}
