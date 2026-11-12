type Props = {
    label: string;
    name: string;
    type?: string;
    autoComplete?: string;
    minLength?: number;
    maxLength?: number;
    value: string;
    onChange: (value: string) => void;
}

export default function FormField({ label, name, type = "text", autoComplete, minLength, maxLength, value, onChange }: Props) {
    return(
        <div>
            <label htmlFor={name} className="mb-1 block text-sm font-medium">{label}</label>
            <input id={name} name={name} type={type} autoComplete={autoComplete} minLength={minLength} maxLength={maxLength} value={value} onChange={(e) => onChange(e.target.value)} required
             className="w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"/>
        </div>
    )
}