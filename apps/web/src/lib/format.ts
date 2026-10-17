export const formatDate = (value: string): string => {
  const dateOnly = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  const date = dateOnly
    ? new Date(Date.UTC(Number(dateOnly[1]), Number(dateOnly[2]) - 1, Number(dateOnly[3])))
    : new Date(value);
  return new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeZone: dateOnly ? 'UTC' : 'Asia/Kolkata' }).format(date);
};
export const formatDateTime = (value: string): string => new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Asia/Kolkata' }).format(new Date(value));
export const formatBytes = (value: number): string => value < 1024 * 1024 ? `${Math.ceil(value / 1024)} KB` : `${(value / 1024 / 1024).toFixed(1)} MB`;
export const sentence = (value: string): string => value.replace(/([a-z])([A-Z])/g, '$1 $2');

export function toLocalInput(value: string): string {
  const parts = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Kolkata', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date(value));
  const part = (type: string) => parts.find(item => item.type === type)?.value ?? '00';
  return `${part('year')}-${part('month')}-${part('day')}T${part('hour')}:${part('minute')}`;
}

export const fromBusinessInput = (value: string): string => new Date(`${value}:00+05:30`).toISOString();

export function localDateKey(date = new Date()): string {
  return toLocalInput(date.toISOString()).slice(0, 10);
}
