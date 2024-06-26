using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.EMMA;

namespace UserInterface.Views;

/// <summary>
/// An internal class representing a property that has been discovered.
/// </summary>
class PropertyMetadata
{
    private readonly object obj;
    private readonly PropertyInfo property;
    private List<string> values;
    private string format;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="model">The model instance</param>
    /// <param name="property">The property info</param>
    /// <param name="alias">The column name to use in the grid.</param>
    /// <param name="units">The units of the property.</param>
    /// <param name="format">The format to use when displaying values in the grid.</param>
    /// <param name="metadata">The metadata for each value.</param>
    public PropertyMetadata(object model, PropertyInfo property, string alias, string units, string format, string[] metadata)
    {
        this.obj = model;
        this.property = property;
        Alias = alias;
        if (Alias == null)
            Alias = property.Name;
        Units = units;
        this.format = format;
        if (metadata != null)
            Metadata = metadata.Select(m => m == "Calculated" || m == "Estimated" ? SheetDataProviderCellState.Calculated : SheetDataProviderCellState.Normal).ToList();
        GetValues();

        if (!property.CanWrite)
            Metadata = Enumerable.Repeat(SheetDataProviderCellState.ReadOnly, Values.Count).ToList();
    }

    /// <summary>The alias of the property.</summary>
    public string Alias { get; }
    
    /// <summary>The units of the property.</summary>
    public string Units { get; }
    
    /// <summary>The metadata of the property.</summary>
    public List<SheetDataProviderCellState> Metadata { get; }

    /// <summary>The values of the property.</summary>
    public ReadOnlyCollection<string> Values => values.AsReadOnly();

    /// <summary>
    /// Set the array values of the property.
    /// </summary>
    /// <param name="valueIndices">The indicies of the rows to change</param>
    /// <param name="values">The new values.</param>
    /// <exception cref="NotImplementedException"></exception>
    public void SetValues(int[] valueIndices, string[] values)
    {
        if (valueIndices.Length != values.Length)
            throw new Exception("The number of row indicies is not equal to the number of values while setting model properties from a grid.");
        bool dataWasChanged = false;
        for (int i = 0; i < valueIndices.Length; i++)
        {
            if (Metadata == null || i >= Metadata.Count || Metadata[i] == SheetDataProviderCellState.Normal)
            {
                while (valueIndices[i] > this.values.Count-1)
                    this.values.Add(null);
                this.values[valueIndices[i]] = values[i];
                dataWasChanged = true;
            }
        }

        if (dataWasChanged)
            SetValues();
    }

    /// <summary>
    /// Get formatted values for the property.
    /// </summary>
    private void GetValues()
    {
        object propertyValue = property.GetValue(obj, null);

        if (property.PropertyType == typeof(string[]))
            values = ((string[])propertyValue).Select(v => v?.ToString()).ToList();
        else if (property.PropertyType == typeof(double[]))
            values = ((double[])propertyValue).Select(v => double.IsNaN(v) ? string.Empty : v.ToString(format)).ToList();
        else if (property.PropertyType == typeof(int[]))
            values = ((int[])propertyValue).Select(v => v.ToString()).ToList();
        else if (property.PropertyType == typeof(DateTime[]))
            values = ((DateTime[])propertyValue).Select(v => v.ToString("yyyy/MM/dd")).ToList();
        else
            throw new Exception($"Unknown property data type found while trying to display model in grid control. Data type: {property.PropertyType}");
    }

    /// <summary>
    /// Set values back to the property.
    /// </summary>
    private void SetValues()
    {
        object newValues = null;

        if (property.PropertyType == typeof(string[]))
            newValues = values.ToArray();
        else if (property.PropertyType == typeof(double[]))
            newValues = values.Select(v => Convert.ToDouble(v)).ToArray();
        else if (property.PropertyType == typeof(int[]))
            newValues = values.Select(v => Convert.ToInt32(v)).ToArray();
        else if (property.PropertyType == typeof(DateTime[]))
            newValues = values.Select(v => DateTime.ParseExact(v, "yyyy/MM/dd", CultureInfo.InvariantCulture)).ToArray();
        else
            throw new Exception($"Unknown property data type found while trying to set values from grid. Data type: {property.PropertyType}");

        property.SetValue(obj, newValues);
    }    
}