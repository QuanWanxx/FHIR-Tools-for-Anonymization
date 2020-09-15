# Evaluation of Mask Processors in Anonymization
[[_TOC_]]


We implement 2 types of processors in [Anonymization](https://github.com/microsoft/FHIR-Tools-for-Anonymization) Tool to mask the value of some Nodes in FHIR data, which indicates to recognize the sensitive entities in a text , and replace them with suitable category those entities belong to.

## Sensitive data in FHIR data
In order to make data in [FHIR](http://hl7.org/fhir/) format accessable, all the identifying information need to be removed to protect patient privacy as [HIPAA](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html#safeharborguidance) requires

Below gives the a part of a data example, the descrption of each fiedls can be found at [FHIR.Resources](http://hl7.org/fhir/resourcelist.html) .
```json
"text": {
    "status": "generated",
    "div": "\u003cdiv xmlns\u003d\"http://www.w3.org/1999/xhtml\"\u003e\n      \n      \u003cp\u003eGastroenterology @ Acme Hospital. ph: +1 555 234 3523, email: \n        \u003ca href\u003d\"mailto:gastro@acme.org\"\u003egastro@acme.org\u003c/a\u003e\n      \u003c/p\u003e\n    \n    \u003c/div\u003e"
  },
  "identifier": [
    {
      "system": "http://www.acme.org.au/units",
      "value": "Gastro"
    }
  ],
  "name": "Gastroenterology",
  "telecom": [
    {
      "system": "phone",
      "value": "+1 555 234 3523",
      "use": "mobile"
    },
    {
      "system": "email",
      "value": "gastro@acme.org",
      "use": "work"
    }
  ],
```

The majority data of FHIR data are structed data, they have clear description indicates what type of each field is, and those data can already be handled well by current released [Anonymization](https://github.com/microsoft/FHIR-Tools-for-Anonymization) Tool. But there exits some none structed fiedls can be filled with any kind of content (represents as a string text), those none structed fields cannot be processed well temporarily, and our task is to handle those fields to improve Anonymization Tool.

The Mask Processors intend to handle the [Narrative](https://www.hl7.org/fhir/narrative.html#Narrative) (the "text" field in previous example) entity in FHIR data. It contains a  human-readable narrative as a string text, which summaries the resource and may be used to represent the content of the resource to a human. We aims to find the identifying entities in narrative and mask them with appropriate field, so that the user will not fell confused when viewing the processed documents.

## NerTAProcessor
To recognize the named entities (Named Entity Recognition) is a classic task in NLP area, and many advanced methods have been developed. NerTAProcessor call the Microsoft [Text Analytics](https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/how-tos/text-analytics-how-to-entity-linking?tabs=version-3-preview) API to recognize the identifying information in Narrative text, and use the recognized category label to replace the raw entities.

The evaluation result of this API on some NLP data sets can be found at 
[here](https://dev.azure.com/boywu/health-ner/_git/health-ner?path=%2Fspecs%2FEvaluation.md&version=GBpersonal%2Fboywu%2Fdoc&_a=preview)

## InspireProcessor
When reviewing the previouse example, we found some content may repeated in struct fields and none struct fields. E.g. The telephone "+1 555 234 3523" and email "gastro@acme.org" appear both in "telecom" field and "text.div“ field.

As we have clear description for struct fiedls in FHIR data, we can match the content and use the description to replace the identifying information in none struct fields.

Here is a simple config for InspireProcess, its target field is "nodesByType('Narrative').div", and for the nodes in "expressions", if thire value can be founds in "nodesByType('Narrative').div", then the content will be replaces with thire description, which we set as their parent node name plus their own node name.


```json
  	{
	  "path": "nodesByType('Narrative').div",
	  "method": "inspire",
	  "expressions": [
		"nodesByType('HumanName')",
		"nodesByType('ContactPoint')",
		"nodesByType('Date')",
		"nodesByType('Datetime')",
		"nodesByType('Address')",
		"nodesByType('Identifier')",
	  ]
	},
```

For the previous example, the processing information and processed result like this
```
[telecom.value] string: , +1 555 234 3523
[telecom.value] string: , gastro@acme.org
[identifier.value] string: , Gastro
```

```json
  "text": {
    "div": "<div xmlns=\"http://www.w3.org/1999/xhtml\">\n      \n      <p>[identifier.value]enterology @ Acme Hospital. ph: [telecom.value], email: \n        <a href=\"mailto:[telecom.value]\">[telecom.value]</a>\n      </p>\n    \n    </div>"
  },
```

## Result
Here are recognize and pending replace content for 2 processors, for readability, we decoded the escape characters and striped the html tags.
```
==========================================================================
{free text content}
//TA result
[{Named Entity Category}]   : {raw text}
--------------
//Insipre result
[{parent.name + own.name}]  {instance type} : {raw text}

==========================================================================
```


```
<div xmlns="http://www.w3.org/1999/xhtml">Everywoman, Eve. SSN:
            444222222</div>

[Phone Number]    : 444222222
-----------------------------
[name.family]   string: , Everywoman
[name.given]    string: , Eve
[identifier.value] string: , 444222222

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: xcda1</p><p><b>identifier</b>: D234123 (OFFICIAL)</p><p><b>name</b>: Sherry Dopplemeyer </p><p><b>telecom</b>: john.doe@healthcare.example.org</p></div>

[Email]           : john.doe@healthcare.example.org
-----------------------------
[name.family]   string: , Dopplemeyer
[name.given]    string: , Sherry
[telecom.value] string: , john.doe@healthcare.example.org
[identifier.value] string: , D234123

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">
      
      <p>Gastroenterology @ Acme Hospital. ph: +1 555 234 3523, email: 
        <a href="mailto:gastro@acme.org">gastro@acme.org</a>
      </p>
    
    </div>

[Phone Number]    : 1 555 234 3523
[Email]           : gastro@acme.org
----------------------------------
[telecom.value] string: , +1 555 234 3523
[telecom.value] string: , gastro@acme.org
[identifier.value] string: , Gastro

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">
      <table>
        <tbody>
          <tr>
            <td>Name</td>
            <td>B‚n‚dicte du March‚</td>
          </tr>
          <tr>
            <td>Address</td>
            <td>43, Place du March‚ Sainte Catherine, 75004 Paris, France</td>
          </tr>
          <tr>
            <td>Contacts</td>
            <td>Phone: +33 (237) 998327</td>
          </tr>
        </tbody>
      </table>
    </div>

[Phone Number]    : (237) 998327
-------------------------
[name.given]    string: , B‚n‚dicte
[telecom.value] string: , +33 (237) 998327
[address.line]  string: , 43, Place du March‚ Sainte Catherine
[address.city]  string: , Paris
[address.postalCode] string: , 75004

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Brian MRI results discussion</div>

[Person]          : Brian MRI
------------------------------

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">A human-readable rendering of the Pharmacy Claim</div>

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">
      <pre>
        Cathy Jones, female. Birth weight 3.25 kg at 44.3 cm. 
        Injection of Vitamin K given on 1972-11-30 (first dose) and 1972-12-11 (second dose)
        Note: Was able to speak Chinese at birth.
      </pre>
    </div>

[Person]          : Cathy Jones
[DateTime]        : 1972-11-30
[DateTime]        : 1972-12-11
-----------------------------

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">
			<table>
				<tbody>
					<tr>
						<td>Name</td>
						<td>Peter James 
              <b>Chalmers</b> ("Jim")
            </td>
					</tr>
					<tr>
						<td>Address</td>
						<td>534 Erewhon, Pleasantville, Vic, 3999</td>
					</tr>
					<tr>
						<td>Contacts</td>
						<td>Home: unknown. Work: (03) 5555 6473</td>
					</tr>
					<tr>
						<td>Id</td>
						<td>MRN: 12345 (Acme Healthcare)</td>
					</tr>
				</tbody>
			</table>
		</div>

[Person]          : Peter James 
                 Chalmers
[Phone Number]    : (03) 5555 6473
[Organization]    : Acme Healthcare
------------------------------------
[name.family]   string: , Chalmers
[name.given]    string: , Peter
[name.given]    string: , James
[name.given]    string: , Jim
[telecom.value] string: , (03) 5555 6473
[address.state] string: , Vic
[address.postalCode] string: , 3999
[identifier.value] string: , 12345
[assigner.display] string: , Acme Healthcare

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: satO2</p><p><b>meta</b>: </p><p><b>identifier</b>: o1223435-10</p><p><b>partOf</b>: <a>Procedure/ob</a></p><p><b>status</b>: final</p><p><b>category</b>: Vital Signs <span>(Details : {http://terminology.hl7.org/CodeSystem/observation-category code 'vital-signs' = 'Vital Signs', given as 'Vital Signs'})</span></p><p><b>code</b>: Oxygen saturation in Arterial blood <span>(Details : {LOINC code '2708-6' = 'Oxygen saturation in Arterial blood', given as 'Oxygen saturation in Arterial blood'}; {LOINC code '59408-5' = 'Oxygen saturation in Arterial blood by Pulse oximetry', given as 'Oxygen saturation in Arterial blood by Pulse oximetry'}; {urn:iso:std:iso:11073:10101 code '150456' = '150456', given as 'MDC_PULS_OXIM_SAT_O2'})</span></p><p><b>subject</b>: <a>Patient/example</a></p><p><b>effective</b>: 05/12/2014 9:30:10 AM</p><p><b>value</b>: 95 %<span> (Details: UCUM code % = '%')</span></p><p><b>interpretation</b>: Normal (applies to non-numeric results) <span>(Details : {http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation code 'N' = 'Normal', given as 'Normal'})</span></p><p><b>device</b>: <a>DeviceMetric/example</a></p><h3>ReferenceRanges</h3><table><tr><td>-</td><td><b>Low</b></td><td><b>High</b></td></tr><tr><td>*</td><td>90 %<span> (Details: UCUM code % = '%')</span></td><td>99 %<span> (Details: UCUM code % = '%')</span></td></tr></table></div>

[URL]             : http://terminology.hl7.org/CodeSystem/observation-category
[URL]             : http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation
-----------------------------
[identifier.value] string: , o1223435-10

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: f003</p><p><b>contained</b>: , </p><p><b>identifier</b>: CP3953 (OFFICIAL)</p><p><b>status</b>: completed</p><p><b>intent</b>: plan</p><p><b>subject</b>: <a>P. van de Heuvel</a></p><p><b>period</b>: 08/03/2013 9:00:10 AM --> 08/03/2013 9:30:10 AM</p><p><b>careTeam</b>: id: careteam</p><p><b>addresses</b>: <a>?????</a></p><p><b>goal</b>: id: goal; lifecycleStatus: completed; Achieved <span>(Details : {http://terminology.hl7.org/CodeSystem/goal-achievement code 'achieved' = 'Achieved', given as 'Achieved'})</span>; Retropharyngeal abscess removal <span>(Details )</span>; Annotation: goal accomplished without complications</p><blockquote><p><b>activity</b></p><h3>Details</h3><table><tr><td>-</td><td><b>Kind</b></td><td><b>Code</b></td><td><b>Status</b></td><td><b>DoNotPerform</b></td><td><b>Scheduled[x]</b></td><td><b>Performer</b></td></tr><tr><td>*</td><td>ServiceRequest</td><td>Incision of retropharyngeal abscess <span>(Details : {SNOMED CT code '172960003' = 'Incision of retropharyngeal abscess', given as 'Incision of retropharyngeal abscess'})</span></td><td>completed</td><td>true</td><td>2011-06-27T09:30:10+01:00</td><td><a>E.M. van den broek</a></td></tr></table></blockquote></div>

[URL]             : http://terminology.hl7.org/CodeSystem/goal-achievement
[Phone Number]    : 172960003
------------------------------
[identifier.value] string: , CP3953
==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Everyman, Adam. SSN:
            444333333</div>

[EU Social Security Number (SSN) or Equivalent ID]: 444333333
---------------------------
[name.family]   string: , Everyman
[name.given]    string: , Adam
[identifier.value] string: , 444333333

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: fda-vcf-comparison</p><p><b>coordinateSystem</b>: 1</p><p><b>patient</b>: <a>Patient/example</a></p><h3>ReferenceSeqs</h3><table><tr><td>-</td><td><b>ReferenceSeqId</b></td><td><b>Strand</b></td><td><b>WindowStart</b></td><td><b>WindowEnd</b></td></tr><tr><td>*</td><td>NC_000001.11 <span>(Details : {http://www.ncbi.nlm.nih.gov/nuccore code 'NC_000001.11' = 'NC_000001.11)</span></td><td>watson</td><td>10453</td><td>101770080</td></tr></table><h3>Variants</h3><table><tr><td>-</td><td><b>Start</b></td><td><b>End</b></td><td><b>ObservedAllele</b></td><td><b>ReferenceAllele</b></td></tr><tr><td>*</td><td>13116</td><td>13117</td><td>T</td><td>G</td></tr></table><h3>Qualities</h3><table><tr><td>-</td><td><b>Type</b></td><td><b>StandardSequence</b></td><td><b>Start</b></td><td><b>End</b></td><td><b>Score</b></td><td><b>Method</b></td><td><b>TruthTP</b></td><td><b>TruthFN</b></td><td><b>QueryFP</b></td><td><b>GtFP</b></td><td><b>Precision</b></td><td><b>FScore</b></td></tr><tr><td>*</td><td>unknown</td><td>file-BkZxBZ00bpJVk2q6x43b1YBx <span>(Details : {https://precision.fda.gov/files/ code 'file-BkZxBZ00bpJVk2q6x43b1YBx' = 'file-BkZxBZ00bpJVk2q6x43b1YBx)</span></td><td>10453</td><td>101770080</td><td>5.000</td><td>VCF Comparison <span>(Details : {https://precision.fda.gov/apps/ code 'app-BqB9XZ8006ZZ2g5KzGXP3fpq' = 'app-BqB9XZ8006ZZ2g5KzGXP3fpq)</span></td><td>129481</td><td>3168</td><td>1507</td><td>2186</td><td>0.9885</td><td>0.9823</td></tr></table><h3>Repositories</h3><table><tr><td>-</td><td><b>Type</b></td><td><b>Url</b></td><td><b>Name</b></td></tr><tr><td>*</td><td>login</td><td><a>https://precision.fda.gov/comparisons/1850</a></td><td>FDA</td></tr></table></div>

[URL]             : http://www.ncbi.nlm.nih.gov/nuccore
[Phone Number]    : 101770080
[URL]             : https://precision.fda.gov/files/
[Phone Number]    : 101770080
[URL]             : https://precision.fda.gov/apps/
[URL]             : https://precision.fda.gov/comparisons/1850
------------------------------

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: protocol</p><p><b>identifier</b>: urn:oid:1.3.6.1.4.1.21367.2005.3.7.1234</p><p><b>status</b>: completed</p><p><b>vaccineCode</b>: Twinrix (HepA/HepB) <span>(Details : {http://hl7.org/fhir/sid/cvx code '104' = 'Hep A-Hep B)</span></p><p><b>patient</b>: <a>Patient/example</a></p><p><b>encounter</b>: <a>Encounter/example</a></p><p><b>occurrence</b>: 18/06/2018</p><p><b>primarySource</b>: true</p><p><b>location</b>: <a>Location/1</a></p><p><b>manufacturer</b>: <a>Organization/hl7</a></p><p><b>lotNumber</b>: PT123F</p><p><b>expirationDate</b>: 15/12/2018</p><p><b>site</b>: left arm <span>(Details : {http://terminology.hl7.org/CodeSystem/v3-ActSite code 'LA' = 'left arm', given as 'left arm'})</span></p><p><b>route</b>: Injection, intramuscular <span>(Details : {http://terminology.hl7.org/CodeSystem/v3-RouteOfAdministration code 'IM' = 'Injection, intramuscular', given as 'Injection, intramuscular'})</span></p><p><b>doseQuantity</b>: 5 mg<span> (Details: UCUM code mg = 'mg')</span></p><blockquote><p><b>performer</b></p><p><b>function</b>: Ordering Provider <span>(Details : {http://terminology.hl7.org/CodeSystem/v2-0443 code 'OP' = 'Ordering Provider)</span></p><p><b>actor</b>: <a>Practitioner/example</a></p></blockquote><blockquote><p><b>performer</b></p><p><b>function</b>: Administering Provider <span>(Details : {http://terminology.hl7.org/CodeSystem/v2-0443 code 'AP' = 'Administering Provider)</span></p><p><b>actor</b>: <a>Practitioner/example</a></p></blockquote><p><b>isSubpotent</b>: false</p><p><b>programEligibility</b>: Not Eligible <span>(Details : {http://terminology.hl7.org/CodeSystem/immunization-program-eligibility code 'ineligible' = 'Not Eligible)</span></p><p><b>fundingSource</b>: Private <span>(Details : {http://terminology.hl7.org/CodeSystem/immunization-funding-source code 'private' = 'Private)</span></p><blockquote><p><b>protocolApplied</b></p><p><b>series</b>: 2-dose</p><p><b>targetDisease</b>: Viral hepatitis, type A <span>(Details : {SNOMED CT code '40468003' = 'Viral hepatitis, type A)</span></p><p><b>doseNumber</b>: 1</p></blockquote><blockquote><p><b>protocolApplied</b></p><p><b>series</b>: 3-dose</p><p><b>targetDisease</b>: Type B viral hepatitis <span>(Details : {SNOMED CT code '66071002' = 'Type B viral hepatitis)</span></p><p><b>doseNumber</b>: 2</p></blockquote></div>

[IP Address]      : 1.3.6.1
[DateTime]        : 2005.3.7
[DateTime]        : 18/06/2018
[DateTime]        : 15/12/2018
[URL]             : http://terminology.hl7.org/CodeSystem/v3-ActSite
[URL]             : http://terminology.hl7.org/CodeSystem/v3-RouteOfAdministration
[URL]             : http://terminology.hl7.org/CodeSystem/v2-0443
[URL]             : http://terminology.hl7.org/CodeSystem/v2-0443
[URL]             : hl7.org/CodeSystem/immunization-program-eligibility
[URL]             : http://terminology.hl7.org/CodeSystem/immunization-funding-source
[Phone Number]    : 40468003
[Phone Number]    : 66071002
------------------------------
[identifier.value] string: , urn:oid:1.3.6.1.4.1.21367.2005.3.7.1234

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: meddisp0322</p><p><b>status</b>: completed</p><p><b>medication</b>: Dilantin 125mg/5ml Oral Suspension <span>(Details : {http://hl7.org/fhir/sid/ndc code '0071-2214-20' = 'n/a', given as 'Dilantin 125mg/5ml Oral Suspension'})</span></p><p><b>subject</b>: <a>Donald Duck</a></p><h3>Performers</h3><table><tr><td>-</td><td><b>Actor</b></td></tr><tr><td>*</td><td><a>Practitioner/f006</a></td></tr></table><p><b>authorizingPrescription</b>: <a>MedicationRequest/medrx0312</a></p><p><b>type</b>: Refill - Part Fill <span>(Details : {http://terminology.hl7.org/CodeSystem/v3-ActCode code 'RFP' = 'Refill - Part Fill', given as 'Refill - Part Fill'})</span></p><p><b>quantity</b>: 360 ml<span> (Details: UCUM code ml = 'ml')</span></p><p><b>daysSupply</b>: 30 Day<span> (Details: UCUM code d = 'd')</span></p><p><b>whenPrepared</b>: 16/01/2015 7:13:00 AM</p><p><b>whenHandedOver</b>: 18/01/2015 7:13:00 AM</p><p><b>dosageInstruction</b>: </p></div>

[URL]             : http://hl7.org/fhir/sid/ndc
[Phone Number]    : 0071-2214-20
[URL]             : http://terminology.hl7.org/CodeSystem/v3-ActCode

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: example-implant</p><p><b>status</b>: completed</p><p><b>code</b>: Implant Pacemaker <span>(Details : {SNOMED CT code '25267002' = 'Insertion of intracardiac pacemaker', given as 'Insertion of intracardiac pacemaker (procedure)'})</span></p><p><b>subject</b>: <a>Patient/example</a></p><p><b>performed</b>: 05/04/2015</p><h3>Performers</h3><table><tr><td>-</td><td><b>Actor</b></td></tr><tr><td>*</td><td><a>Dr Cecil Surgeon</a></td></tr></table><p><b>reasonCode</b>: Bradycardia <span>(Details )</span></p><p><b>followUp</b>: ROS 5 days  - 2013-04-10 <span>(Details )</span></p><p><b>note</b>: Routine Appendectomy. Appendix was inflamed and in retro-caecal position</p><h3>FocalDevices</h3><table><tr><td>-</td><td><b>Action</b></td><td><b>Manipulated</b></td></tr><tr><td>*</td><td>Implanted <span>(Details : {http://hl7.org/fhir/device-action code 'implanted' = 'Implanted)</span></td><td><a>Device/example-pacemaker</a></td></tr></table></div>

[Phone Number]    : 25267002
[DateTime]        : 05/04/2015
[URL]             : http://hl7.org/fhir/device-action
------------------------------

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Kidd, Kari. SSN:
            444555555</div>

[Person]          : Kidd
[Phone Number]    : 444555555
---------------------------
[name.family]   string: , Kidd
[name.given]    string: , Kari
[identifier.value] string: , 444555555

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">
			<p>
				<b> Generated Narrative with Details</b>
			</p>
		</div>

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: example3</p><p><b>status</b>: draft</p><p><b>intent</b>: order</p><p><b>code</b>: Refill Request <span>(Details )</span></p><p><b>focus</b>: <a>MedicationRequest/medrx002</a></p><p><b>for</b>: <a>Patient/f001</a></p><p><b>authoredOn</b>: 10/03/2016 10:39:32 PM</p><p><b>lastModified</b>: 10/03/2016 10:39:32 PM</p><p><b>requester</b>: <a>Patient/example</a></p><p><b>owner</b>: <a>Practitioner/example</a></p></div>

[DateTime]        : 10/03/2016
------------------------------

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Nuclear, Nancy. SSN:
            444114567</div>

[EU Social Security Number (SSN) or Equivalent ID]: 444114567
------------------------------
[name.family]   string: , Nuclear
[name.given]    string: , Nancy
[identifier.value] string: , 444114567

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: f204</p><p><b>identifier</b>: 15970</p><p><b>category</b>: Chemical <span>(Details : {http://terminology.hl7.org/CodeSystem/substance-category code 'chemical' = 'Chemical', given as 'Chemical'})</span></p><p><b>code</b>: Silver nitrate 20% solution (product) <span>(Details : {SNOMED CT code '333346007' = 'Silver nitrate 20% solution', given as 'Silver nitrate 20% solution (product)'})</span></p><p><b>description</b>: Solution for silver nitrate stain</p><h3>Instances</h3><table><tr><td>-</td><td><b>Identifier</b></td><td><b>Expiry</b></td><td><b>Quantity</b></td></tr><tr><td>*</td><td>AB94687</td><td>01/01/2018</td><td>100 mL<span> (Details: UCUM code mL = 'mL')</span></td></tr></table></div>

[URL]             : http://terminology.hl7.org/CodeSystem/substance-category
[Phone Number]    : 333346007
[DateTime]        : 01/01/2018
------------------------------
[identifier.value] string: , 15970
[identifier.value] string: , AB94687

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: lipid</p><p><b>contained</b>: , </p><p><b>identifier</b>: Placer = 2345234234234</p><p><b>status</b>: active</p><p><b>intent</b>: original-order</p><p><b>code</b>: Lipid Panel <span>(Details : {http://acme.org/tests code 'LIPID' = 'LIPID)</span></p><p><b>subject</b>: <a>Patient/example</a></p><p><b>encounter</b>: <a>Encounter/example</a></p><p><b>occurrence</b>: 02/05/2013 4:16:00 PM</p><p><b>requester</b>: <a>Practitioner/example</a></p><p><b>performer</b>: <a>Practitioner/f202</a></p><p><b>reasonCode</b>: Fam hx-ischem heart dis <span>(Details : {ICD-9 code 'V173' = 'V173', given as 'Fam hx-ischem heart dis'})</span></p><p><b>supportingInfo</b>: Fasting status. Generated Summary: id: fasting; status: final; Fasting status - Reported <span>(Details : {LOINC code '49541-6' = 'Fasting status - Reported', given as 'Fasting status - Reported'})</span>; Yes <span>(Details : {http://terminology.hl7.org/CodeSystem/v2-0136 code 'Y' = 'Yes', given as 'Yes'})</span></p><p><b>specimen</b>: Serum specimen. Generated Summary: id: serum; 20150107-0012; Serum sample <span>(Details : {SNOMED CT code '119364003' = 'Serum specimen', given as 'Serum sample'})</span></p><p><b>note</b>: patient is afraid of needles</p></div>

[Phone Number]    : 2345234234234
[URL]             : http://acme.org/tests
[Phone Number]    : 20150107-0012
[U.S. Driver's License Number]: '
[Phone Number]    : 119364003
[U.S. Driver's License Number]: '
---------------------------
[type.text]     string: , Placer
[identifier.value] string: , 2345234234234

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml"><p><b>Generated Narrative with Details</b></p><p><b>id</b>: isolate</p><p><b>contained</b>: </p><p><b>accessionIdentifier</b>: X352356-ISO1</p><p><b>status</b>: available</p><p><b>type</b>: Bacterial isolate specimen <span>(Details : {SNOMED CT code '429951000124103' = 'Bacterial isolate specimen (specimen)', given as 'Bacterial isolate specimen'})</span></p><p><b>subject</b>: <a>Patient/example</a></p><p><b>receivedTime</b>: 18/08/2015 7:03:00 AM</p><p><b>parent</b>: id: stool; X352356; status: unavailable; Stool specimen <span>(Details : {SNOMED CT code '119339001' = 'Stool specimen', given as 'Stool specimen'})</span>; receivedTime: 16/08/2015 7:03:00 AM</p><h3>Collections</h3><table><tr><td>-</td><td><b>Collector</b></td><td><b>Collected[x]</b></td><td><b>Method</b></td></tr><tr><td>*</td><td><a>Practitioner/f202</a></td><td>16/08/2015 7:03:00 AM</td><td>Plates, Blood Agar <span>(Details : {http://terminology.hl7.org/CodeSystem/v2-0488 code 'BAP' = 'Plates, Blood Agar)</span></td></tr></table><p><b>note</b>: Patient dropped off specimen</p></div>

[Phone Number]    : 429951000124103
[U.S. Driver's License Number]: '
[Phone Number]    : 119339001
[U.S. Driver's License Number]: '
---------------------------------
[accessionIdentifier.value] string: , X352356-ISO1

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Nuclear, Neville. SSN:
            444111234</div>

[Phone Number]    : 444111234
------------------------------
[name.family]   string: , Nuclear
[name.given]    string: , Neville
[identifier.value] string: , 444111234

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Nuclear, Ned. SSN:
            444113456</div>

[Phone Number]    : 444113456
------------------------------
[name.family]   string: , Nuclear
[name.given]    string: , Ned
[identifier.value] string: , 444113456

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Nuclear, Nelda. SSN:
            444112345</div>

[Phone Number]    : 444112345
------------------------------
[name.family]   string: , Nuclear
[name.given]    string: , Nelda
[identifier.value] string: , 444112345

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Mum, Martha. SSN:
            444666666</div>

[Phone Number]    : 444666666
------------------------------
[name.family]   string: , Mum
[name.given]    string: , Martha
[identifier.value] string: , 444666666

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Sons, Stuart. SSN:
            444777777</div>

[Phone Number]    : 444777777
------------------------------
[name.family]   string: , Sons
[name.given]    string: , Stuart
[identifier.value] string: , 444777777

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Betterhalf, Boris. SSN:
            444888888</div>

[EU Social Security Number (SSN) or Equivalent ID]: 444888888
---------------------------
[name.family]   string: , Betterhalf
[name.given]    string: , Boris
[identifier.value] string: , 444888888

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Relative, Ralph. SSN:
            444999999</div>

[Phone Number]    : 444999999
-----------------------------
[name.family]   string: , Relative
[name.given]    string: , Ralph
[identifier.value] string: , 444999999

==========================================================================
<div xmlns="http://www.w3.org/1999/xhtml">Contact, Carrie. SSN:
            555222222</div>

[Phone Number]    : 555222222
-----------------------------
[name.family]   string: , Contact
[name.given]    string: , Carrie
[identifier.value] string: , 555222222

==========================================================================
```





