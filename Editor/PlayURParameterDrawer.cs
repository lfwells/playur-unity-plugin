//a unity inspector for the PlayURParameterAttribute
using PlayUR;
using System.Linq;
using UnityEditor;
using UnityEngine;

//note this wont display for 

[CustomPropertyDrawer(typeof(PlayURParameterAttribute))]
public class PlayURParameterDrawer : PropertyDrawer
{
    bool isArrayElement(SerializedProperty property)
    {
        return property.propertyPath.Split(".").Last().StartsWith("data[");
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (isArrayElement(property))
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var attribute = (PlayURParameterAttribute)base.attribute;
        //display the property as a label indicating it is a PlayURParameter and what the parameter name is

        var bounds = position;

        position.width = EditorGUIUtility.labelWidth;
        position.height = EditorGUIUtility.singleLineHeight;
        GUI.Label(position, label);

        position.x += position.width;
        position.width = bounds.width - position.x;
        GUI.Label(position, new GUIContent { text = $"PlayURParameter: {attribute.key}", tooltip = "To change this, edit the attribute in code" }, EditorStyles.boldLabel);

        //next row
        position.y += position.height;
        position.x = EditorGUIUtility.labelWidth;
        position.width = 60;
        GUI.Label(position, "Default:");

        position.x += 60;
        position.width = bounds.width - position.x;
        EditorGUI.PropertyField(position, property, GUIContent.none);
    }

    //two lines high
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (isArrayElement(property)) return base.GetPropertyHeight(property, label);
        return base.GetPropertyHeight(property, label) * 2;
    }
}