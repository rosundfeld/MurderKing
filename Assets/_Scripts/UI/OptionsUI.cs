using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class OptionsUI : MonoBehaviour
{
   [Header("Volume Buttons")]
   [SerializeField] private Button soundEffectsButton;
   [SerializeField] private Button musicButton;
   [SerializeField] private TextMeshProUGUI soundEffectsButtonText;
   [SerializeField] private TextMeshProUGUI musicButtonText;

   //----------------KeyBind Buttons----------------------
   [Header("Key Bind Buttons")]
   [SerializeField] private Button moveUpButton;
   [SerializeField] private TextMeshProUGUI moveUpButtonText;
   [SerializeField] private Button moveDownButton;
   [SerializeField] private TextMeshProUGUI moveDownButtonText;
   [SerializeField] private Button moveLeftButton;
   [SerializeField] private TextMeshProUGUI moveLeftButtonText;
   [SerializeField] private Button moveRightButton;
   [SerializeField] private TextMeshProUGUI moveRightButtonText;
   [SerializeField] private Button interactionButton;
   [SerializeField] private TextMeshProUGUI interactionButtonText;
   [SerializeField] private Button alternateInteractionButton;
   [SerializeField] private TextMeshProUGUI alternateInteractionButtonText;
   [SerializeField] private Button pauseButton;
   [SerializeField] private TextMeshProUGUI pauseButtonText;

   //----------------Gamepad Buttons----------------------
   [Header("Gamepad Buttons")]
   [SerializeField] private Button gamepadInteractButton;
   [SerializeField] private TextMeshProUGUI gamepadInteractButtonText;
   [SerializeField] private Button gamepadAlternateInteractionButton;
   [SerializeField] private TextMeshProUGUI gamepadAlternateInteractionButtonText;
   [SerializeField] private Button gamepadPauseButton;
   [SerializeField] private TextMeshProUGUI gamepadPauseButtonText;

   [Header("Close Button")]
   [SerializeField] private Button closeButton;

   [Header("Press a Key Panel")]
   [SerializeField] private Transform pressAKeyPanelTransform;

   private Action onCloseButtonAction;

   public static OptionsUI Instance { get; private set; }


   private void Awake()
   {
      Instance = this;
      soundEffectsButton.onClick.AddListener(() =>
      {
         SoundManager.Instance.ChangeVolume();
         UpdateVisual();
      });

      musicButton.onClick.AddListener(() =>
      {
         MusicManager.Instance.ChangeVolume();
         UpdateVisual();
      });

      closeButton.onClick.AddListener(() =>
      {
         Hide();
         onCloseButtonAction();
      });


      //--------------Rebind Buttons----------------------
      moveUpButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Move_Up);
      });

      moveDownButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Move_Down);
      });

      moveLeftButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Move_Left);
      });

      moveRightButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Move_Right);
      });

      interactionButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Interact);
      });

      alternateInteractionButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Fire);
      });

      pauseButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Pause);
      });


      gamepadInteractButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Gamepad_Interact);
      });

      gamepadAlternateInteractionButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Gamepad_Fire);
      });

      gamepadPauseButton.onClick.AddListener(() =>
      {
         RebindBinding(GameInput.Binding.Gamepad_Pause);
      });
   }

   private void Start()
   {
      GameManager.Instance.OnLocalGameUnpaused += GameManager_OnGameUnpaused;
      UpdateVisual();

      Hide();
      HidePressAKeyPanel();
   }

   private void GameManager_OnGameUnpaused(object sender, System.EventArgs e)
   {
      Hide();
   }

   private void UpdateVisual()
   {
      soundEffectsButtonText.text = "Sound Effects: " + Mathf.Round(SoundManager.Instance.GetVolume() * 10f);
      musicButtonText.text = "Music: " + Mathf.Round(MusicManager.Instance.GetVolume() * 10f);

      moveUpButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Up);
      moveDownButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Down);
      moveLeftButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Left);
      moveRightButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Right);
      interactionButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Interact);
      alternateInteractionButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Fire);
      pauseButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Pause);

      gamepadInteractButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Interact);
      gamepadAlternateInteractionButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Fire);
      gamepadPauseButtonText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Pause);
   }

   public void Show(Action onCloseButtonAction)
   {
      gameObject.SetActive(true);

      this.onCloseButtonAction = onCloseButtonAction;
      soundEffectsButton.Select();
   }

   private void Hide()
   {
      gameObject.SetActive(false);
   }


   private void ShowPressAKeyPanel()
   {
      pressAKeyPanelTransform.gameObject.SetActive(true);
   }

   private void HidePressAKeyPanel()
   {
      pressAKeyPanelTransform.gameObject.SetActive(false);
   }

   private void RebindBinding(GameInput.Binding binding)
   {
      ShowPressAKeyPanel();
      GameInput.Instance.RebindBinding(binding, () =>
      {
         HidePressAKeyPanel();
         UpdateVisual();
      });
   }

}
