﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Interface;
using ImGuiNET;

namespace KamiLib.AutomaticUserInterface;

public abstract class DrawableAttribute : FieldAttributeBase
{
    private static readonly Dictionary<Type, IOrderedEnumerable<IGrouping<OrderData, AttributeData>>> AttributeCache = new();
    private record OrderData(int Group, string Label);
    private record AttributeData(FieldInfo Field, DrawableAttribute DrawableAttribute);
    protected DrawableAttribute(string? label, string category, int group) : base(label, category, group) { }
    protected abstract void Draw(object obj, FieldInfo field, Action? saveAction = null);

    private static IOrderedEnumerable<IGrouping<OrderData, AttributeData>> GetSortedObjectAttributes(Type obj)
    {
        if (!AttributeCache.ContainsKey(obj))
        {
            var attributes = GetAttributes(obj);
            
            var objectAttributes = attributes
                .GroupBy(attributeInfo => new OrderData(attributeInfo.DrawableAttribute.GroupIndex, attributeInfo.DrawableAttribute.Category))
                .OrderBy(orderData => orderData.Key.Group);

            AttributeCache.Add(obj, objectAttributes);
        }

        return AttributeCache[obj];
    }

    private static IEnumerable<AttributeData> GetAttributes(Type obj) => 
        from field in obj.GetFields() 
        where field.IsDefined(typeof(DrawableAttribute), true) 
        let drawableAttribute = field.GetCustomAttribute<DrawableAttribute>() 
        where drawableAttribute is not null 
        select new AttributeData(field, drawableAttribute);

    public static void DrawAttributes(object obj, Action? saveAction = null)
    {
        var cachedAttributes = GetSortedObjectAttributes(obj.GetType());

        foreach (var categoryGroup in cachedAttributes)
        {
            ImGui.Text(categoryGroup.Key.Label);
            ImGui.Separator();
            ImGuiHelpers.ScaledIndent(15.0f);

            ImGui.PushID(categoryGroup.Key.Label);
            foreach (var attributeData in categoryGroup)
            {
                attributeData.DrawableAttribute.Draw(obj, attributeData.Field, saveAction);
            }
            ImGui.PopID();
            
            ImGuiHelpers.ScaledDummy(10.0f);
            ImGuiHelpers.ScaledIndent(-15.0f);
        }
        
        foreach (var nestedField in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (nestedField.GetValue(obj) is { } nested)
            {
                DrawAttributes(nested, saveAction);
            }
        }
    }
}