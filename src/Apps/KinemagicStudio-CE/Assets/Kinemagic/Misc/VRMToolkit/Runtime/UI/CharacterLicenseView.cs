using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniGLTF.Extensions.VRMC_vrm;
using UnityEngine;
using UnityEngine.UIElements;
using UniVRM10.Migration;
using Vrm10Meta = UniGLTF.Extensions.VRMC_vrm.Meta;

namespace VRMToolkit.UI
{
    public sealed class CharacterLicenseView : MonoBehaviour
    {
        public enum DialogResult
        {
            Accept,
            Decline
        }

        public enum CharacterModelType
        {
            Vrm10,
            Vrm0
        }

        // Constants for UI element names
        private const string OpenUrlButtonName = "open-url-button";
        private const string DialogContainerName = "dialog-container";
        private const string DialogTitleName = "dialog-title";
        private const string DialogHeaderTextName = "dialog-header-text";
        private const string AgreementToggleName = "agreement-toggle";
        private const string LanguageButtonName = "language-button";
        private const string AcceptButtonName = "accept-button";
        private const string DeclineButtonName = "decline-button";
        private const string ThumbnailImageName = "thumbnail-image";
        private const string ModelFormatLabelName = "model-format-label";
        private const string ModelFormatValueName = "model-format-value";
        private const string ModelNameLabelName = "model-name-label";
        private const string ModelNameValueName = "model-name-value";
        private const string VersionLabelName = "version-label";
        private const string VersionValueName = "version-value";
        private const string AuthorsLabelName = "authors-label";
        private const string AuthorsValueName = "authors-value";
        private const string CopyrightLabelName = "copyright-label";
        private const string CopyrightValueName = "copyright-value";
        private const string ContactLabelName = "contact-label";
        private const string ContactValueName = "contact-value";
        private const string ReferenceLabelName = "reference-label";
        private const string ReferenceValueName = "reference-value";
        private const string AvatarUseLabelName = "avatar-use-label";
        private const string AvatarUseValueName = "avatar-use-value";
        private const string ViolentUsageLabelName = "violent-usage-label";
        private const string ViolentUsageValueName = "violent-usage-value";
        private const string SexualUsageLabelName = "sexual-usage-label";
        private const string SexualUsageValueName = "sexual-usage-value";
        private const string PoliticalOrReligiousUsageLabelName = "political-or-religious-usage-label";
        private const string PoliticalOrReligiousUsageValueName = "political-or-religious-usage-value";
        private const string AntisocialOrHateUsageLabelName = "antisocial-or-hate-usage-label";
        private const string AntisocialOrHateUsageValueName = "antisocial-or-hate-usage-value";
        private const string CommercialUseLabelName = "commercial-use-label";
        private const string CommercialUseValueName = "commercial-use-value";
        private const string OtherPermissionLabelName = "other-permission-label";
        private const string OtherPermissionValueName = "other-permission-value";
        private const string AttributionLabelName = "attribution-label";
        private const string AttributionValueName = "attribution-value";
        private const string RedistributionLabelName = "redistribution-label";
        private const string RedistributionValueName = "redistribution-value";
        private const string ModificationLabelName = "modification-label";
        private const string ModificationValueName = "modification-value";
        private const string LicenseLabelName = "license-label";
        private const string LicenseValueName = "license-value";
        private const string ThirdPartyLicenseLabelName = "third-party-license-label";
        private const string ThirdPartyLicenseValueName = "third-party-license-value";
        private const string OtherLicenseUrlLabelName = "other-license-url-label";
        private const string OtherLicenseUrlValueName = "other-license-url-value";

        [SerializeField] private UIDocument _uiDocument;

        private readonly Dictionary<string, Button> _urlButtons = new();

        private bool _initialized;
        private Action<DialogResult> _onResultAction;
        private CharacterModelType _currentCharacterModelType = CharacterModelType.Vrm10;

        // UI Elements
        private VisualElement _dialogContainer;
        private Label _dialogTitle;
        private Label _dialogHeaderText;
        private Button _languageButton;
        private Toggle _agreementToggle;
        private Button _acceptButton;
        private Button _declineButton;
        private Image _thumbnailImage;
        private Label _modelFormatLabel;
        private Label _modelFormatValue;
        private Label _modelNameLabel;
        private Label _modelNameValue;
        private Label _versionLabel;
        private Label _versionValue;
        private Label _authorsLabel;
        private Label _authorsValue;
        private Label _copyrightLabel;
        private Label _copyrightValue;
        private Label _contactLabel;
        private Label _contactValue;
        private Label _referenceLabel;
        private Label _referenceValue;
        private Label _avatarUseLabel;
        private Label _avatarUseValue;
        private Label _violentUsageLabel;
        private Label _violentUsageValue;
        private Label _sexualUsageLabel;
        private Label _sexualUsageValue;
        private Label _politicalOrReligiousUsageLabel;
        private Label _politicalOrReligiousUsageValue;
        private Label _antisocialOrHateUsageLabel;
        private Label _antisocialOrHateUsageValue;
        private Label _commercialUseLabel;
        private Label _commercialUseValue;
        private Label _otherPermissionLabel;
        private Label _otherPermissionValue;
        private Label _attributionLabel;
        private Label _attributionValue;
        private Label _redistributionLabel;
        private Label _redistributionValue;
        private Label _modificationLabel;
        private Label _modificationValue;
        private Label _licenseLabel;
        private Label _licenseValue;
        private Label _thirdPartyLicenseLabel;
        private Label _thirdPartyLicenseValue;
        private Label _otherLicenseUrlLabel;
        private Label _otherLicenseUrlValue;

        #region MonoBehaviour Methods
        
        private void Awake() // Awake?
        {
            if (_initialized) return;

            var rootElement = _uiDocument.rootVisualElement;

            // Get UI elements
            _dialogContainer = rootElement.Q<VisualElement>(DialogContainerName);
            _dialogTitle = rootElement.Q<Label>(DialogTitleName);
            _dialogHeaderText = rootElement.Q<Label>(DialogHeaderTextName);
            _agreementToggle = rootElement.Q<Toggle>(AgreementToggleName);
            _languageButton = rootElement.Q<Button>(LanguageButtonName);
            _acceptButton = rootElement.Q<Button>(AcceptButtonName);
            _declineButton = rootElement.Q<Button>(DeclineButtonName);
            _thumbnailImage = rootElement.Q<Image>(ThumbnailImageName);
            _modelFormatLabel = rootElement.Q<Label>(ModelFormatLabelName);
            _modelFormatValue = rootElement.Q<Label>(ModelFormatValueName);
            _modelNameLabel = rootElement.Q<Label>(ModelNameLabelName);
            _modelNameValue = rootElement.Q<Label>(ModelNameValueName);
            _versionLabel = rootElement.Q<Label>(VersionLabelName);
            _versionValue = rootElement.Q<Label>(VersionValueName);
            _authorsLabel = rootElement.Q<Label>(AuthorsLabelName);
            _authorsValue = rootElement.Q<Label>(AuthorsValueName);
            _copyrightLabel = rootElement.Q<Label>(CopyrightLabelName);
            _copyrightValue = rootElement.Q<Label>(CopyrightValueName);
            _contactLabel = rootElement.Q<Label>(ContactLabelName);
            _contactValue = rootElement.Q<Label>(ContactValueName);
            _referenceLabel = rootElement.Q<Label>(ReferenceLabelName);
            _referenceValue = rootElement.Q<Label>(ReferenceValueName);
            _avatarUseLabel = rootElement.Q<Label>(AvatarUseLabelName);
            _avatarUseValue = rootElement.Q<Label>(AvatarUseValueName);
            _violentUsageLabel = rootElement.Q<Label>(ViolentUsageLabelName);
            _violentUsageValue = rootElement.Q<Label>(ViolentUsageValueName);
            _sexualUsageLabel = rootElement.Q<Label>(SexualUsageLabelName);
            _sexualUsageValue = rootElement.Q<Label>(SexualUsageValueName);
            _politicalOrReligiousUsageLabel = rootElement.Q<Label>(PoliticalOrReligiousUsageLabelName);
            _politicalOrReligiousUsageValue = rootElement.Q<Label>(PoliticalOrReligiousUsageValueName);
            _antisocialOrHateUsageLabel = rootElement.Q<Label>(AntisocialOrHateUsageLabelName);
            _antisocialOrHateUsageValue = rootElement.Q<Label>(AntisocialOrHateUsageValueName);
            _commercialUseLabel = rootElement.Q<Label>(CommercialUseLabelName);
            _commercialUseValue = rootElement.Q<Label>(CommercialUseValueName);
            _otherPermissionLabel = rootElement.Q<Label>(OtherPermissionLabelName);
            _otherPermissionValue = rootElement.Q<Label>(OtherPermissionValueName);
            _attributionLabel = rootElement.Q<Label>(AttributionLabelName);
            _attributionValue = rootElement.Q<Label>(AttributionValueName);
            _redistributionLabel = rootElement.Q<Label>(RedistributionLabelName);
            _redistributionValue = rootElement.Q<Label>(RedistributionValueName);
            _modificationLabel = rootElement.Q<Label>(ModificationLabelName);
            _modificationValue = rootElement.Q<Label>(ModificationValueName);
            _licenseLabel = rootElement.Q<Label>(LicenseLabelName);
            _licenseValue = rootElement.Q<Label>(LicenseValueName);
            _thirdPartyLicenseLabel = rootElement.Q<Label>(ThirdPartyLicenseLabelName);
            _thirdPartyLicenseValue = rootElement.Q<Label>(ThirdPartyLicenseValueName);
            _otherLicenseUrlLabel = rootElement.Q<Label>(OtherLicenseUrlLabelName);
            _otherLicenseUrlValue = rootElement.Q<Label>(OtherLicenseUrlValueName);

            // Set initial state
            _acceptButton.SetEnabled(false);
            UpdateLabels(CharacterModelType.Vrm10);
            Hide();

            _initialized = true;
        }
        
        private void OnEnable()
        {
            _agreementToggle.RegisterValueChangedCallback(OnAgreementToggleChanged);
            _languageButton.clicked += OnLanguageButtonClicked;
            _acceptButton.clicked += OnAcceptButtonClicked;
            _declineButton.clicked += OnDeclineButtonClicked;
        }

        private void OnDisable()
        {
            _agreementToggle.UnregisterValueChangedCallback(OnAgreementToggleChanged);
            _languageButton.clicked -= OnLanguageButtonClicked;
            _acceptButton.clicked -= OnAcceptButtonClicked;
            _declineButton.clicked -= OnDeclineButtonClicked;
        }

        #endregion
        
        #region Public Methods

        public void Show(Texture2D thumbnail, ICharacterModelInfo characterModelInfo, Action<DialogResult> onResult)
        {
            _onResultAction = onResult;
            
            // Clear state
            _agreementToggle.value = false;
            _acceptButton.SetEnabled(false);
            
            // Clear any existing URL buttons
            foreach (var button in _urlButtons.Values)
            {
                button.RemoveFromHierarchy();
            }
            _urlButtons.Clear();
            
            if (characterModelInfo is VrmCharacterModelInfo vrmCharacterModelInfo)
            {
                if (vrmCharacterModelInfo.Metadata.Vrm10Meta != null)
                {
                    _currentCharacterModelType = CharacterModelType.Vrm10;
                    UpdateLabels(_currentCharacterModelType);
                    ShowVrm10License(vrmCharacterModelInfo.Metadata.Vrm10Meta);
                }
                else if (vrmCharacterModelInfo.Metadata.Vrm0Meta != null)
                {
                    _currentCharacterModelType = CharacterModelType.Vrm0;
                    UpdateLabels(_currentCharacterModelType);
                    ShowVrm0License(vrmCharacterModelInfo.Metadata.Vrm0Meta);
                }
            }
            
            _thumbnailImage.image = thumbnail;
            _dialogContainer.style.display = DisplayStyle.Flex;
        }
        
        public void Hide()
        {
            _dialogContainer.style.display = DisplayStyle.None;
            _onResultAction = null;
        }

        #endregion

        private void ShowVrm10License(Vrm10Meta meta)
        {
            _modelFormatValue.text = "VRM 1.0";
            _modelNameValue.text = meta.Name;
            _versionValue.text = meta.Version;
            _copyrightValue.text = meta.CopyrightInformation;

            var authors = "";
            for (var i = 0; i < meta.Authors.Count; i++)
            {
                if (i > 0) authors += ", ";
                authors += meta.Authors[i];
            }
            _authorsValue.text = authors;

            _contactValue.text = TruncateIfNeeded(meta.ContactInformation, 45);
            AddOpenUrlButtonIfNeeded(_contactValue, meta.ContactInformation, "contact");

            var reference = "";
            if (meta.References != null)
            {
                for (var i = 0; i < meta.References.Count; i++)
                {
                    if (i > 0) reference += System.Environment.NewLine;
                    reference += meta.References[i];
                }
            }
            _referenceValue.text = TruncateIfNeeded(reference, 45);
            AddOpenUrlButtonIfNeeded(_referenceValue, reference, "reference");

            _avatarUseValue.text = meta.AvatarPermission.ToString();
            _violentUsageValue.text = meta.AllowExcessivelyViolentUsage.ToString();
            _sexualUsageValue.text = meta.AllowExcessivelySexualUsage.ToString();
            _politicalOrReligiousUsageValue.text = meta.AllowPoliticalOrReligiousUsage.ToString();
            _antisocialOrHateUsageValue.text = meta.AllowAntisocialOrHateUsage.ToString();
            _commercialUseValue.text = meta.CommercialUsage.ToString();
            _otherPermissionValue.text = "";

            _attributionValue.text = meta.CreditNotation.ToString();
            _redistributionValue.text = meta.AllowRedistribution.ToString();
            
            var modificationType = meta.Modification;
            _modificationValue.text = modificationType switch
            {
                ModificationType.prohibited => LocalizationManager.GetText("vrm10.modification.prohibited"),
                ModificationType.allowModification => LocalizationManager.GetText("vrm10.modification.allow_modification"),
                ModificationType.allowModificationRedistribution => LocalizationManager.GetText("vrm10.modification.allow_modification_redistribution"),
                _ => "Unknown"
            };

            _licenseValue.text = TruncateIfNeeded(meta.LicenseUrl, 85);
            AddOpenUrlButtonIfNeeded(_licenseValue, meta.LicenseUrl, "license");

            _thirdPartyLicenseValue.text = TruncateIfNeeded(meta.ThirdPartyLicenses, 85);
            AddOpenUrlButtonIfNeeded(_thirdPartyLicenseValue, meta.ThirdPartyLicenses, "third_party_license");

            _otherLicenseUrlValue.text = TruncateIfNeeded(meta.OtherLicenseUrl, 85);
            AddOpenUrlButtonIfNeeded(_otherLicenseUrlValue, meta.OtherLicenseUrl, "other_license");
        }

        private void ShowVrm0License(Vrm0Meta meta)
        {
            _modelFormatValue.text = "VRM 0.x";
            _modelNameValue.text = meta.title;
            _versionValue.text = meta.version;
            _authorsValue.text = meta.author;

            var contactInfo = meta.contactInformation;
            _contactValue.text = TruncateIfNeeded(contactInfo, 45);
            AddOpenUrlButtonIfNeeded(_contactValue, contactInfo, "contact");

            var reference = meta.reference;
            _referenceValue.text = TruncateIfNeeded(reference, 45);
            AddOpenUrlButtonIfNeeded(_referenceValue, reference, "reference");

            _avatarUseValue.text = meta.allowedUser.ToString();
            _violentUsageValue.text = meta.violentUsage.ToString();
            _sexualUsageValue.text = meta.sexualUsage.ToString();
            _politicalOrReligiousUsageValue.text = "";
            _antisocialOrHateUsageValue.text = "";
            _commercialUseValue.text = meta.commercialUsage.ToString();

            var otherPermission = meta.otherPermissionUrl;
            _otherPermissionValue.text = TruncateIfNeeded(otherPermission, 85);
            AddOpenUrlButtonIfNeeded(_otherPermissionValue, otherPermission, "permission");

            _attributionValue.text = "";
            _redistributionValue.text = "";
            _modificationValue.text = "";

            var license = meta.licenseType.ToString();
            if (meta.licenseType == LicenseType.Redistribution_Prohibited)
            {
                license = LocalizationManager.GetText("vrm0x.license.redistribution_prohibited");
            }
            _licenseValue.text = TruncateIfNeeded(license, 85);
            AddOpenUrlButtonIfNeeded(_licenseValue, license, "license");

            _thirdPartyLicenseLabel.text = "";

            _otherLicenseUrlValue.text = TruncateIfNeeded(meta.otherLicenseUrl, 85);
            AddOpenUrlButtonIfNeeded(_otherLicenseUrlValue, meta.otherLicenseUrl, "license");
        }

        private void UpdateLabels(CharacterModelType characterModelType)
        {
            _dialogTitle.text = LocalizationManager.GetText("character_license.title");
            _dialogHeaderText.text = LocalizationManager.GetText("character_license.header_text");
            _agreementToggle.text = LocalizationManager.GetText("character_license.agreement");
            _acceptButton.text = LocalizationManager.GetText("character_license.accept_button");
            _declineButton.text = LocalizationManager.GetText("character_license.decline_button");
            _modelFormatLabel.text = LocalizationManager.GetText("character_license.model_format");

            if (characterModelType == CharacterModelType.Vrm0)
            {
                _modelNameLabel.text = LocalizationManager.GetText("vrm0x.title");
                _versionLabel.text = LocalizationManager.GetText("vrm0x.version");
                _authorsLabel.text = LocalizationManager.GetText("vrm0x.authors");
                _contactLabel.text = LocalizationManager.GetText("vrm0x.contact_information");
                _referenceLabel.text = LocalizationManager.GetText("vrm0x.references");
                _avatarUseLabel.text = LocalizationManager.GetText("vrm0x.allowed_user");
                _violentUsageLabel.text = LocalizationManager.GetText("vrm0x.violent_usage");
                _sexualUsageLabel.text = LocalizationManager.GetText("vrm0x.sexual_usage");
                _politicalOrReligiousUsageLabel.text = "";
                _antisocialOrHateUsageLabel.text = "";
                _commercialUseLabel.text = LocalizationManager.GetText("vrm0x.commercial_usage");
                _otherPermissionLabel.text = LocalizationManager.GetText("vrm0x.other_permission_url");
                _attributionLabel.text = "";
                _redistributionLabel.text = "";
                _modificationLabel.text = "";
                _licenseLabel.text = LocalizationManager.GetText("vrm0x.license_type");
                _thirdPartyLicenseLabel.text = "";
                _otherLicenseUrlLabel.text = LocalizationManager.GetText("vrm0x.other_license_url");
            }
            else
            {
                _modelNameLabel.text = LocalizationManager.GetText("vrm10.name");
                _versionLabel.text = LocalizationManager.GetText("vrm10.version");
                _authorsLabel.text = LocalizationManager.GetText("vrm10.authors");
                _copyrightLabel.text = LocalizationManager.GetText("vrm10.copyright_information");
                _contactLabel.text = LocalizationManager.GetText("vrm10.contact_information");
                _referenceLabel.text = LocalizationManager.GetText("vrm10.references");
                _avatarUseLabel.text = LocalizationManager.GetText("vrm10.avatar_permission");
                _violentUsageLabel.text = LocalizationManager.GetText("vrm10.violent_usage");
                _sexualUsageLabel.text = LocalizationManager.GetText("vrm10.sexual_usage");
                _politicalOrReligiousUsageLabel.text = LocalizationManager.GetText("vrm10.political_or_religious_usage");
                _antisocialOrHateUsageLabel.text = LocalizationManager.GetText("vrm10.antisocial_or_hate_usage");
                _commercialUseLabel.text = LocalizationManager.GetText("vrm10.commercial_usage");
                _otherPermissionLabel.text = "";
                _attributionLabel.text = LocalizationManager.GetText("vrm10.attribution");
                _redistributionLabel.text = LocalizationManager.GetText("vrm10.redistribution");
                _modificationLabel.text = LocalizationManager.GetText("vrm10.modification");
                _licenseLabel.text = LocalizationManager.GetText("vrm10.license_url");
                _thirdPartyLicenseLabel.text = LocalizationManager.GetText("vrm10.third_party_license");
                _otherLicenseUrlLabel.text = LocalizationManager.GetText("vrm10.other_license_url");
            }
        }

        private string TruncateIfNeeded(string text, int maxTextLength, string truncationSuffix = "...")
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= maxTextLength) return text;
            return text.Substring(0, maxTextLength) + truncationSuffix;
        }

        private void AddOpenUrlButtonIfNeeded(Label targetLabel, string originalText, string uniqueId)
        {
            if (string.IsNullOrEmpty(originalText)) return;

            var match = Regex.Match(originalText, @"https?://[^\s<>""]+");
            var url = match.Success ? match.Value : string.Empty;
            if (string.IsNullOrEmpty(url)) return;

            var buttonId = $"{OpenUrlButtonName}-{uniqueId}";
            var openUrlButton = new Button { text = "Open URL", name = buttonId };
            openUrlButton.AddToClassList("open-url-button");
            openUrlButton.clicked += () => Application.OpenURL(url);

            targetLabel.parent.Add(openUrlButton);
            _urlButtons[buttonId] = openUrlButton;
        }

        private void OnLanguageButtonClicked()
        {
            var isEnglish = LocalizationManager.CurrentLanguage == LocalizationManager.Language.English;
            LocalizationManager.CurrentLanguage = isEnglish ? LocalizationManager.Language.Japanese
                                                            : LocalizationManager.Language.English;
            UpdateLabels(_currentCharacterModelType);
        }

        private void OnAgreementToggleChanged(ChangeEvent<bool> evt)
        {
            _acceptButton.SetEnabled(evt.newValue);
        }
        
        private void OnAcceptButtonClicked()
        {
            _onResultAction?.Invoke(DialogResult.Accept);
            Hide();
        }
        
        private void OnDeclineButtonClicked()
        {
            _onResultAction?.Invoke(DialogResult.Decline);
            Hide();
        }
    }
}