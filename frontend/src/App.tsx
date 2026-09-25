import { useEffect, useState } from 'react';
import { HomeScreen } from './components/HomeScreen';
import { BuilderScreen } from './components/BuilderScreen';
import { Stage2Workspace } from './components/Stage2Workspace';
import { AppCategory } from './types';

type Stage = 'home' | 'builder' | 'workstation';

export default function App() {
  const [stage, setStage] = useState<Stage>('home');
  const [directive, setDirective] = useState('');
  const [appType, setAppType] = useState<AppCategory>('web');
  const [runKey, setRunKey] = useState(0);

  const goHome = () => setStage('home');

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && e.altKey && stage !== 'home') goHome();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [stage]);

  return (
    <div className="w-screen h-screen overflow-hidden bg-[#03060C] text-[#F5F8FF] selection:bg-[#2F7CFF]/30 font-sans">
      {stage === 'home' && (
        <HomeScreen
          onSubmit={(text) => {
            setDirective(text);
            setStage('builder');
          }}
          onOpenBuilder={() => setStage('builder')}
        />
      )}

      {stage === 'builder' && (
        <BuilderScreen
          initialDirective={directive}
          onBuild={(text, type) => {
            setDirective(text);
            setAppType(type);
            setRunKey((k) => k + 1);
            setStage('workstation');
          }}
        />
      )}

      {stage === 'workstation' && (
        <Stage2Workspace
          key={runKey}
          initialPrompt={directive}
          appType={appType}
          onReturnHome={goHome}
          onOpenIntake={() => setStage('builder')}
        />
      )}
    </div>
  );
}
