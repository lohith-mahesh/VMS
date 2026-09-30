import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { EmptyState, StatusTag } from './Ui';

describe('UI components', () => {
  it('renders accessible empty-state content', () => {
    render(<MemoryRouter><EmptyState title="No requests" message="Nothing is waiting." /></MemoryRouter>);
    expect(screen.getByRole('heading', { name: 'No requests' })).toBeInTheDocument();
    expect(screen.getByText('Nothing is waiting.')).toBeInTheDocument();
  });

  it('formats a camel-case status', () => {
    render(<StatusTag value="PendingScreening" />);
    expect(screen.getByText('Pending Screening')).toBeInTheDocument();
  });
});
