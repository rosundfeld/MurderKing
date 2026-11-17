using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    //---------------CONSTANTS-----------------
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    //---------------ENUMS-----------------
    public enum Binding
    {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        Fire,
        Reload,
        Pause,
        Gamepad_Interact,
        Gamepad_Fire,
        Gamepad_Reload,
        Gamepad_Pause
    }

    //---------------SINGLETON-----------------
    public static GameInput Instance { get; private set; }

    //---------------EVENTS-----------------
    public event EventHandler OnInteractAction;
    public event EventHandler OnFireAction;
    public event EventHandler OnReloadAction;
    public event EventHandler OnWeaponSwapAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnBindingRebind;

    //---------------PRIVATE VARIABLES-----------------
    private PlayerInputActions playerInputActions;
    private bool lastFireFromGamepad;
    //shortcut: bound in code, so it can't be rebound in the options menu; move it into the PlayerInputActions asset when that matters
    private readonly InputAction weaponSwapAction = new InputAction("WeaponSwap", InputActionType.Button, "<Keyboard>/q");

    //---------------UNITY METHODS-----------------

    private void Awake()
    {
        Instance = this;
        playerInputActions = new PlayerInputActions();
        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        playerInputActions.Player.Enable();

        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.Fire.performed += Fire_performed;
        playerInputActions.Player.Reload.performed += Reload_performed;
        playerInputActions.Player.Pause.performed += Pause_performed;

        weaponSwapAction.AddBinding("<Gamepad>/rightShoulder");
        weaponSwapAction.performed += WeaponSwap_performed;
        weaponSwapAction.Enable();
    }

    private void OnDestroy()
    {
        playerInputActions.Player.Interact.performed -= Interact_performed;
        playerInputActions.Player.Fire.performed -= Fire_performed;
        playerInputActions.Player.Reload.performed -= Reload_performed;
        playerInputActions.Player.Pause.performed -= Pause_performed;

        playerInputActions.Player.Disable();
        playerInputActions.Dispose();
        weaponSwapAction.Dispose();
    }

    private void WeaponSwap_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnWeaponSwapAction?.Invoke(this, EventArgs.Empty);
    }

    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    private void Fire_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        lastFireFromGamepad = obj.control.device is Gamepad;
        OnFireAction?.Invoke(this, EventArgs.Empty);
    }

    private void Reload_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnReloadAction?.Invoke(this, EventArgs.Empty);
    }

    //False when the last shot came from a gamepad, which has no cursor to aim with
    public bool TryGetAimScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = default;
        if (lastFireFromGamepad || Mouse.current == null)
        {
            return false;
        }
        screenPosition = Mouse.current.position.ReadValue();
        return true;
    }

    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();

        inputVector = inputVector.normalized;

        return inputVector;
    }


    public string GetBindingText(Binding binding)
    {
        switch (binding)
        {
            default:
            case Binding.Move_Up:
                return playerInputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return playerInputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return playerInputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return playerInputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return playerInputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.Fire:
                return playerInputActions.Player.Fire.bindings[0].ToDisplayString();
            case Binding.Pause:
                return playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            case Binding.Gamepad_Interact:
                return playerInputActions.Player.Interact.bindings[1].ToDisplayString();
            case Binding.Gamepad_Fire:
                return playerInputActions.Player.Fire.bindings[1].ToDisplayString();
            case Binding.Gamepad_Pause:
                return playerInputActions.Player.Pause.bindings[1].ToDisplayString();
        }
    }

    public void RebindBinding(Binding binding, Action onActionRebound)
    {
        playerInputActions.Player.Disable();

        InputAction inputAction;
        int bindingIndex;

        switch (binding)
        {
            default:
            case Binding.Move_Up:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.Fire:
                inputAction = playerInputActions.Player.Fire;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Fire:
                inputAction = playerInputActions.Player.Fire;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 1;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback =>
            {
                callback.Dispose();
                playerInputActions.Player.Enable();
                onActionRebound();

                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start();
    }
}
