using System;
using System.Xml.Linq;

using NUnit.Framework;

using uSync.Core;

namespace uSync.Tests.Extensions;

[TestFixture]
public class XmlExtensionTests
{
    XElement _node;
    XElement _blankNode;
    XElement _emptyNode;

    Guid _key = Guid.NewGuid();
    string _alias = "alias";
    int _level = 2;
    string _cultures = "en-gb,en-us";
    string _segments = "testa,testb";
    SyncActionType _actionType = SyncActionType.Delete;

    string _defaultValues = "defaultvalue";
    string _attributeValue = "attribute";
    string _nodeValue = "node value";

    [SetUp]
    public void Setup()
    {
        _node = XElement.Parse("<Nodes " +
            $"   Key=\"{_key}\"" +
            $"   Alias=\"{_alias}\"" +
            $"   Level=\"{_level}\" " +
            $"   Cultures=\"{_cultures}\" " +
            $"   Segments=\"{_segments}\" >" +
            $"  <Container name=\"{_attributeValue}\">{_nodeValue}</Container>" +
            $"  <String>{_nodeValue}</String> " +
            $"  <Integer>{_level}</Integer> " +
            $"  <Guid>{_key}</Guid> " +
            $"  <Blank name=\"\" ></Blank>" +
            "</Nodes>");

        _blankNode = XElement.Parse("<nodes/>");

        _emptyNode = XElement.Parse("<Empty " +
            $"Key=\"{_key}\" " +
            $"Alias=\"{_alias}\" " +
            $"Change=\"{_actionType}\" " +
            $"/>");

    }

    [Test]
    public void Key_Value_From_Node_Attribute()
    {
        var key = _node.GetKey();
        Assert.AreEqual(_key, key);
    }


    [Test]
    public void Key_Value_Blank_When_Missing()
    {
        var blank = _blankNode.GetKey();
        Assert.AreEqual(Guid.Empty, blank);
    }

    [Test]
    public void Alias_Value_From_Node_Attribute()
    {
        var alias = _node.GetAlias();
        Assert.AreEqual(_alias, alias);
    }


    [Test]
    public void Alias_Value_Blank_When_Missing()
    {
        var blank = _blankNode.GetAlias();
        Assert.AreEqual(string.Empty, blank);
    }

    [Test]
    public void Level_Value_From_Node_Attribute()
    {
        var level = _node.GetLevel();
        Assert.AreEqual(_level, level);
    }

    [Test]
    public void Level_Value_Zero_When_Missing()
    {
        var defaultLevel = _blankNode.GetLevel();
        Assert.AreEqual(0, defaultLevel);
    }

    [Test]
    public void Cultures_Value_From_Node_Attribute()
    {
        var cultures = _node.GetCultures();
        Assert.AreEqual(_cultures, cultures);
    }

    [Test]
    public void Segments_Value_From_Node_Attribute()
    {
        var segments = _node.GetSegments();
        Assert.AreEqual(_segments, segments);
    }

    [Test]
    public void Is_EmptyNode_When_Empty()
    {
        var isEmpty = _emptyNode.IsEmptyItem();
        Assert.IsTrue(isEmpty);
    }

    [Test]
    public void IsNot_EmptyNode_When_Normal()
    {
        var isNotEmpty = _node.IsEmptyItem();
        Assert.IsFalse(isNotEmpty);
    }

    [Test]
    public void MakeEmpty_Makes_EmptyNode()
    {
        var e = XElementExtensions.MakeEmpty(_key, SyncActionType.Delete, _alias);
        Assert.AreEqual(_emptyNode.ToString(), e.ToString());
    }

    [Test]
    public void GetEmptyAction_Is_Delete_When_Set()
    {
        var action = _emptyNode.GetEmptyAction();
        Assert.AreEqual(_actionType, action);
    }

    [Test]
    public void GetEmptyAction_Is_None_When_Not_Empty()
    {
        SyncActionType noAction = _node.GetEmptyAction();
        Assert.AreEqual(SyncActionType.None, noAction);
    }

    [Test]
    public void Value_Or_Default_IsValue()
    {
        var value = _node.Element("Container")
            .ValueOrDefault(_defaultValues);

        Assert.AreEqual(_nodeValue, value);
    }


    [Test]
    public void Value_Or_Default_IsDefault_When_Null()
    {
        var missing = _node.Element("NoNode")
            .ValueOrDefault(_defaultValues);

        Assert.AreEqual(missing, _defaultValues);
    }

    [Test]
    public void Value_Or_Default_IsDefault_When_Blank()
    {
        var blank = _node.Element("Blank")
            .ValueOrDefault(_defaultValues);

        Assert.AreEqual(blank, _defaultValues);
    }

    [Test]
    public void Value_or_Default_Is_Interger()
    {
        var value = _node.Element("Integer")
            .ValueOrDefault(0);

        Assert.IsInstanceOf<int>(value);
        Assert.AreEqual(value, _level);
    }

    [Test]
    public void Value_or_Default_Is_Guid()
    {
        var value = _node.Element("Guid")
            .ValueOrDefault(Guid.Empty);

        Assert.IsInstanceOf<Guid>(value);
        Assert.AreEqual(_key, value);
    }
    [Test]
    public void Value_or_Default_Is_String()
    {
        var value = _node.Element("String")
            .ValueOrDefault(_defaultValues);

        Assert.IsInstanceOf<string>(value);
        Assert.AreEqual(_nodeValue, value);
    }
}
