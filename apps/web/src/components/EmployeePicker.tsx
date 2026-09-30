import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import { Field, TextInput } from './Ui';

interface Employee {
  objectId: string;
  displayName: string;
  email: string;
  department: string;
}

export function EmployeePicker({
  label,
  namePrefix,
  defaultObjectId = '',
  defaultName = '',
  defaultDepartment = '',
  required = false
}: {
  label: string;
  namePrefix: 'mainHost' | 'escortingHost';
  defaultObjectId?: string;
  defaultName?: string;
  defaultDepartment?: string;
  required?: boolean;
}) {
  const [name, setName] = useState(defaultName);
  const [objectId, setObjectId] = useState(defaultObjectId);
  const [department, setDepartment] = useState(defaultDepartment);
  const [results, setResults] = useState<Employee[]>([]);
  const [searching, setSearching] = useState(false);
  useEffect(() => {
    if (objectId || name.trim().length < 2 || name === defaultName) {
      setResults([]);
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setSearching(true);
      api<Employee[]>(`/api/directory?query=${encodeURIComponent(name.trim())}`, { signal: controller.signal })
        .then(setResults)
        .catch(() => setResults([]))
        .finally(() => setSearching(false));
    }, 250);
    return () => { controller.abort(); window.clearTimeout(timer); };
  }, [defaultName, name, objectId]);
  return <div className="employee-picker">
    <Field label={label} hint={searching ? 'Searching employee directory…' : 'Type two or more characters to search the configured directory.'} required={required}>
      <TextInput value={name} onChange={event => { setName(event.target.value); setObjectId(''); }} maxLength={200} required={required} autoComplete="off" />
    </Field>
    {results.length > 0 && <div className="employee-results" role="listbox" aria-label={`${label} suggestions`}>{results.map(employee => <button key={employee.objectId} type="button" role="option" aria-selected="false" onClick={() => { setName(employee.displayName); setObjectId(employee.objectId); setDepartment(employee.department); setResults([]); }}><strong>{employee.displayName}</strong><span>{employee.email}{employee.department ? ` · ${employee.department}` : ''}</span></button>)}</div>}
    <Field label={`${label} object ID`} hint="Required when the directory is unavailable." required={required}><TextInput name={`${namePrefix}ObjectId`} value={objectId} onChange={event => setObjectId(event.target.value)} maxLength={64} required={required} /></Field>
    <input type="hidden" name={`${namePrefix}Name`} value={name} />
    {namePrefix === 'mainHost' && <Field label="Host department"><TextInput name="hostDepartment" value={department} onChange={event => setDepartment(event.target.value)} maxLength={200} /></Field>}
  </div>;
}
