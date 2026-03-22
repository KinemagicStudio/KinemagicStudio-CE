using System;
using System.Collections.Generic;
using CinematicSequencer.Animation;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using FloatField = UnityEngine.UIElements.FloatField;
namespace CinematicSequencer.UI.KeyframeEditor
{
    /// <summary>
    /// プロパティ値の編集パネル。AnimationPropertyDescriptorに基づいてUIコントロールを自動生成する。
    /// </summary>
    public sealed class PropertyEditorPanel : VisualElement
    {
        private readonly Dictionary<string, VisualElement> _fields = new();

        public event Action<string, float> ValueChanged;

        public PropertyEditorPanel()
        {
            AddToClassList("property-editor-panel");
        }

        public void SetProperties(IReadOnlyList<AnimationPropertyDescriptor> descriptors)
        {
            Clear();
            _fields.Clear();

            string currentGroup = null;

            for (int i = 0; i < descriptors.Count; i++)
            {
                var desc = descriptors[i];

                // Group separator
                if (!string.IsNullOrEmpty(desc.Group) && desc.Group != currentGroup)
                {
                    currentGroup = desc.Group;
                    var groupLabel = new Label(currentGroup);
                    groupLabel.AddToClassList("property-group-header");
                    Add(groupLabel);
                }

                var row = new VisualElement();
                row.AddToClassList("property-row");

                var label = new Label(desc.DisplayName ?? desc.Name);
                label.AddToClassList("property-label");
                row.Add(label);

                VisualElement field;
                if (desc.MinValue.HasValue && desc.MaxValue.HasValue)
                {
                    field = CreateSlider(desc);
                }
                else
                {
                    field = CreateFloatField(desc);
                }

                row.Add(field);
                Add(row);
                _fields[desc.Name] = field;
            }
        }

        public void UpdateValues(AnimationFrame frame, bool editable)
        {
            if (frame == null) return;

            for (int i = 0; i < frame.Properties.Count; i++)
            {
                var (name, value) = frame.GetProperty(i);
                if (!_fields.TryGetValue(name, out var field)) continue;

                switch (field)
                {
                    case SliderFloat slider:
                        slider.SetValueWithoutNotify(value);
                        slider.SetEnabled(editable);
                        break;
                    case FloatField floatField:
                        floatField.SetValueWithoutNotify(value);
                        floatField.SetEnabled(editable);
                        break;
                }
            }
        }

        private SliderFloat CreateSlider(AnimationPropertyDescriptor desc)
        {
            var slider = new SliderFloat
            {
                lowValue = desc.MinValue.Value,
                highValue = desc.MaxValue.Value,
                value = desc.DefaultValue,
                showInputField = true
            };
            slider.AddToClassList("property-slider");

            var propertyName = desc.Name;
            slider.RegisterValueChangedCallback(evt =>
            {
                ValueChanged?.Invoke(propertyName, evt.newValue);
            });
            return slider;
        }

        private FloatField CreateFloatField(AnimationPropertyDescriptor desc)
        {
            var field = new FloatField { value = desc.DefaultValue };
            field.AddToClassList("property-float-field");

            var propertyName = desc.Name;
            field.RegisterValueChangedCallback(evt =>
            {
                ValueChanged?.Invoke(propertyName, evt.newValue);
            });
            return field;
        }
    }
}
