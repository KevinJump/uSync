namespace Jumoo.uSync.Core.Validation
{
    public class Schemas
    {
        public const string MemberSchema =
@"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:element name='Member'>
        <xs:complexType>
            <xs:sequence>
                <xs:element name='General'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='Name' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Key' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='ContentTypeAlias' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Username' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Email' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='RawPassword' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                           <xs:element name='RawPasswordAnswer' type='xs:string' minOccurs='1' maxOccurs='1'/>
                        </xs:sequence>
                    </xs:complexType> 
                </xs:element>
                <xs:element name='Properties'
                            type='Properties'
                            minOccurs='1'
                            maxOccurs='1' />
                <xs:element name='Roles'
                            type='Roles'
                            minOccurs='1'
                            maxOccurs='1' />
            </xs:sequence>
        </xs:complexType>
    </xs:element>
    <xs:complexType name='Roles'>
        <xs:sequence>
            <xs:element name='Role'
                        type='Role'
                        minOccurs='0'
                        maxOccurs='unbounded'/>
        </xs:sequence>
    </xs:complexType>
    <xs:simpleType name='Role'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='1'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:complexType name='Properties'>
        <xs:sequence>
            <xs:any namespace='##any' processContents='lax' minOccurs='0' maxOccurs='unbounded' />
        </xs:sequence>
    </xs:complexType>
</xs:schema>
";

        public const string MemberGroupSchema =
@"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:element name='MemberGroup'>
        <xs:complexType>
            <xs:sequence>
                <xs:element name='General'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='Name' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Key' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                        </xs:sequence>
                    </xs:complexType> 
                </xs:element>
            </xs:sequence>
        </xs:complexType>
    </xs:element>
</xs:schema>
";

        public const string UserSchema =
@"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:element name='User'>
        <xs:complexType>
            <xs:sequence>
                <xs:element name='General'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='Name' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Username' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Email' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Language' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='UserEnabled' type='xs:boolean' minOccurs='1' maxOccurs='1' />
                            <xs:element name='UmbracoAccessDisabled' type='xs:boolean' minOccurs='1' maxOccurs='1' />
                            <xs:element name='StartContentNodeKey' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='StartMediaNodeKey' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='UserTypeAlias' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='RawPassword' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='RawPasswordAnswer' minOccurs='0' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                        </xs:sequence>
                    </xs:complexType> 
                </xs:element>
                <xs:element name='AllowedSections'
                            type='AllowedSections'
                            minOccurs='1'
                            maxOccurs='1' />
                <xs:element name='NodePermissions'
                            type='NodePermissions'
                            minOccurs='1'
                            maxOccurs='1' />
            </xs:sequence>
        </xs:complexType>
    </xs:element>
    <xs:complexType name='AllowedSections'>
        <xs:sequence>
            <xs:element name='AllowedSection'
                        type='AllowedSection'
                        minOccurs='0'
                        maxOccurs='unbounded'/>
        </xs:sequence>
    </xs:complexType>
    <xs:simpleType name='AllowedSection'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='1'/>
        </xs:restriction>
    </xs:simpleType>
    <xs:complexType name='NodePermissions'>
        <xs:sequence>
            <xs:element name='NodePermission'
                        type='NodePermission'
                        minOccurs='0'
                        maxOccurs='unbounded'/>
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name='NodePermission'>
        <xs:sequence>
            <xs:element name='Permission'
                        type='Permission'
                        minOccurs='0'
                        maxOccurs='unbounded'/>
        </xs:sequence>
        <xs:attribute name='nodeKey' use='required'>
            <xs:simpleType>
                <xs:restriction base='xs:string'>
                    <xs:minLength value='1'/>
                </xs:restriction>
            </xs:simpleType>
        </xs:attribute>
    </xs:complexType>
    <xs:simpleType name='Permission'>
        <xs:restriction base='xs:string'>
            <xs:length value='1'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";

        public const string UserTypeSchema =
@"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:element name='UserType'>
        <xs:complexType>
            <xs:sequence>
                <xs:element name='General'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='Name' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                            <xs:element name='Alias' minOccurs='1' maxOccurs='1'>
                                <xs:simpleType>
                                    <xs:restriction base='xs:string'>
                                        <xs:minLength value='1'/>
                                    </xs:restriction>
                                </xs:simpleType>
                            </xs:element>
                        </xs:sequence>
                    </xs:complexType> 
                </xs:element>
                <xs:element name='Permissions'
                            type='Permissions'
                            minOccurs='1'
                            maxOccurs='1' />
            </xs:sequence>
        </xs:complexType>
    </xs:element>
    <xs:complexType name='Permissions'>
        <xs:sequence>
            <xs:element name='Permission'
                        type='Permission'
                        minOccurs='0'
                        maxOccurs='unbounded'/>
        </xs:sequence>
    </xs:complexType>
    <xs:simpleType name='Permission'>
        <xs:restriction base='xs:string'>
            <xs:minLength value='1'/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>
";
    }
}